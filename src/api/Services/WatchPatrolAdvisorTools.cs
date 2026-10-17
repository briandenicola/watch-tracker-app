using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using WatchTracker.Api.DTOs;

namespace WatchTracker.Api.Services;

public class WatchPatrolAdvisorTools(
    IWatchPatrolMcpClient client,
    ILogger<WatchPatrolAdvisorTools> logger)
{
    private const int MaxResultLength = 8_000;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly Regex Urls = new(
        @"https://[^\s<>""\\]+", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));

    public static bool IsAllowed(Tool tool) =>
        tool.Annotations is { ReadOnlyHint: true, DestructiveHint: not true }
        && !new[] { "identify", "photo", "image", "alert", "star", "export", "bulk" }
            .Any(word => tool.Name.Contains(word, StringComparison.OrdinalIgnoreCase))
        && new[] { "get_", "list_", "search_", "compare_", "market_", "price_" }
            .Any(prefix => tool.Name.StartsWith(prefix, StringComparison.Ordinal))
        && new[] { "brand", "model", "listing", "pric", "seller", "market" }
            .Any(word => tool.Name.Contains(word, StringComparison.OrdinalIgnoreCase));

    public async Task<AdvisorToolResult> ExecuteAsync(
        string toolName, JsonElement arguments, AdvisorToolContext context, CancellationToken ct)
    {
        try
        {
            if (toolName == "watchpatrol_tools")
            {
                context.WatchPatrolTools.Clear();
                var available = await client.ListToolsAsync(context.UserId, ct);
                var approvedTools = available.Where(IsAllowed).ToList();
                if (approvedTools.Count == 0)
                    return Failure(toolName, "unavailable",
                        "WatchPatrol advertised no supported, explicitly read-only research tools.");
                if (approvedTools.Select(tool => tool.Name).Distinct().Count() != approvedTools.Count)
                    throw new InvalidOperationException("WatchPatrol returned duplicate tool names.");
                var output = JsonSerializer.Serialize(new
                {
                    provider = "WatchPatrol",
                    tools = approvedTools.Select(tool => new
                    {
                        tool.Name,
                        tool.Description,
                        tool.InputSchema
                    }),
                    message = "Discovery metadata only, not market evidence. Use watchpatrol_call with an exact listed name and schema."
                }, JsonOptions);
                EnsureBound(output, 20_000);
                foreach (var tool in approvedTools)
                    context.WatchPatrolTools.Add(tool.Name, tool);
                return Result(toolName, output, "completed");
            }

            if (!arguments.TryGetProperty("name", out var nameElement)
                || nameElement.ValueKind != JsonValueKind.String
                || nameElement.GetString() is not string name
                || !context.WatchPatrolTools.TryGetValue(name, out var approved)
                || !IsAllowed(approved))
                throw new InvalidOperationException(
                    "Discover WatchPatrol tools first, then call an exact approved read-only tool name.");
            if (!arguments.TryGetProperty("arguments", out var input)
                || input.ValueKind != JsonValueKind.Object || input.GetRawText().Length > 2000)
                throw new InvalidOperationException("WatchPatrol tool arguments must be a JSON object under 2,000 characters.");

            var response = await client.CallToolAsync(context.UserId, name, input, ct);
            if (response.IsError == true)
                return Failure(toolName, "failed",
                    "WatchPatrol rejected the lookup. Check the tool's arguments or reconnect; its usage limit may have been reached.");
            var texts = response.Content.OfType<TextContentBlock>().Select(block => block.Text).ToList();
            var evidence = response.StructuredContent is JsonElement structured
                ? JsonSerializer.Serialize(structured, new JsonSerializerOptions { WriteIndented = true })
                : string.Join("\n", texts);
            if (string.IsNullOrWhiteSpace(evidence))
                return Failure(toolName, "unavailable", "WatchPatrol returned no text or structured research evidence.");
            EnsureBound(evidence);
            var observedAt = DateTime.UtcNow;
            var sourceUrls = Urls.Matches(evidence).Select(match => match.Value.TrimEnd('.', ',', ')', ']'))
                .Where(url => Uri.TryCreate(url, UriKind.Absolute, out var uri)
                    && uri.Scheme == "https" && uri.UserInfo.Length == 0)
                .Distinct().Take(5).ToList();
            // The known provider is the attribution when aggregate data contains no links.
            sourceUrls.Insert(0, WatchPatrolConnectionService.Origin + "/");
            var outputJson = JsonSerializer.Serialize(new
            {
                provider = "WatchPatrol",
                tool = name,
                observedAt,
                sourceUrls,
                priceType = "asking prices, not confirmed sales",
                evidence
            }, JsonOptions);
            EnsureBound(outputJson, 12_000);
            foreach (var url in sourceUrls)
                context.Sources[url] = new AdvisorCitationDto
                {
                    Title = $"WatchPatrol: {name}",
                    Url = url,
                    Provider = "WatchPatrol",
                    ObservedAt = observedAt
                };
            return new AdvisorToolResult(outputJson, new AdvisorToolActivityDto
            {
                Tool = toolName,
                Status = "completed",
                Message = $"WatchPatrol: {name}. Asking-price research only; see the observed source result.",
                Evidence = evidence
            });
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return Failure(toolName, "failed", "The WatchPatrol lookup timed out. Try again later.");
        }
        catch (Exception ex) when (ex is HttpRequestException or McpException or TimeoutException or ArgumentException
            or InvalidOperationException or JsonException)
        {
            logger.LogWarning("WatchPatrol interactive tool failed ({ErrorType}).", ex.GetType().Name);
            var message = ex switch
            {
                HttpRequestException { StatusCode: HttpStatusCode.TooManyRequests } =>
                    "WatchPatrol's free usage limit was reached. Wait before trying again.",
                HttpRequestException { StatusCode: HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden } =>
                    "WatchPatrol authorization was rejected. Disconnect and connect again.",
                InvalidOperationException => ex.Message,
                TimeoutException => "The WatchPatrol lookup timed out. Try again later.",
                _ => "WatchPatrol could not complete the interactive lookup. Try again later."
            };
            return Failure(toolName, "failed", message);
        }
    }

    private static void EnsureBound(string value, int maximum = MaxResultLength)
    {
        if (value.Length > maximum)
            throw new InvalidOperationException(
                "WatchPatrol returned too much data. Narrow the search or request fewer results.");
    }

    private AdvisorToolResult Failure(string name, string status, string message)
    {
        logger.LogWarning("WatchPatrol lookup was not completed: {Reason}", message);
        return Result(name, JsonSerializer.Serialize(new { error = message }, JsonOptions), status, message);
    }

    private static AdvisorToolResult Result(string name, string json, string status, string? message = null) =>
        new(json, new AdvisorToolActivityDto { Tool = name, Status = status, Message = message });
}
