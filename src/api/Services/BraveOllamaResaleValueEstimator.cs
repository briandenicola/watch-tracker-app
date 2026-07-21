using System.Text;
using System.Text.Json;
using WatchTracker.Api.Models;

namespace WatchTracker.Api.Services;

public class BraveOllamaResaleValueEstimator(
    IBraveSearchClient braveSearchClient,
    IAppSettingsService appSettings,
    HttpClient httpClient,
    ILogger<BraveOllamaResaleValueEstimator> logger) : IResaleValueEstimator
{
    private const string SourceName = "Web Search Estimate";

    public async Task<ResaleEstimateResult?> EstimateAsync(Watch watch, CancellationToken ct = default)
    {
        var query = $"{watch.Brand} {watch.Model} watch resale price used";
        var results = await braveSearchClient.SearchAsync(query, ct);
        if (results.Count == 0)
        {
            logger.LogInformation("No Brave Search results for {Brand} {Model}; skipping estimate.", watch.Brand, watch.Model);
            return null;
        }

        var ollamaUrl = await appSettings.GetAsync(AppSettingsService.Keys.OllamaUrl, "http://localhost:11434");
        var model = await appSettings.GetAsync(AppSettingsService.Keys.OllamaModel);
        if (string.IsNullOrWhiteSpace(ollamaUrl) || string.IsNullOrWhiteSpace(model))
        {
            logger.LogInformation("Ollama is not configured; skipping web search estimate leg.");
            return null;
        }

        var promptTemplate = await appSettings.GetAsync(AppSettingsService.Keys.ResaleValuePrompt);
        var snippets = string.Join("\n", results.Select(r => $"- {r.Title}: {r.Description} ({r.Url})"));
        var purchaseContext = watch.PurchasePrice is decimal price
            ? $" It was purchased new/used for approximately {price:C}."
            : "";

        var prompt =
            $"{promptTemplate}\n\n" +
            $"Watch: {watch.Brand} {watch.Model}.{purchaseContext}\n\n" +
            $"Web search results about this watch's resale/secondhand listings:\n{snippets}\n\n" +
            "Respond with ONLY a JSON object, no other text, in exactly this shape: " +
            "{\"estimatedValue\": <number>, \"reasoning\": \"<brief explanation>\"}";

        try
        {
            var content = await CallOllamaAsync(ollamaUrl, model, prompt, ct);
            var parsed = ParseEstimate(content);
            if (parsed is null)
            {
                logger.LogWarning("Could not parse a resale value estimate from Ollama response for {Brand} {Model}.", watch.Brand, watch.Model);
                return null;
            }

            return new ResaleEstimateResult(parsed.Value.Value, parsed.Value.Reasoning, SourceName);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Web search resale value estimate failed for {Brand} {Model}.", watch.Brand, watch.Model);
            return null;
        }
    }

    private async Task<string> CallOllamaAsync(string ollamaUrl, string model, string prompt, CancellationToken ct)
    {
        var requestBody = new
        {
            model,
            messages = new[]
            {
                new { role = "user", content = prompt }
            },
            stream = false
        };

        var json = JsonSerializer.Serialize(requestBody);
        var request = new HttpRequestMessage(HttpMethod.Post, $"{ollamaUrl.TrimEnd('/')}/api/chat")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        var response = await httpClient.SendAsync(request, ct);
        var responseBody = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Ollama API error: {responseBody}");

        using var doc = JsonDocument.Parse(responseBody);
        return doc.RootElement.GetProperty("message").GetProperty("content").GetString()
            ?? throw new InvalidOperationException("No content in Ollama response.");
    }

    private static (decimal Value, string? Reasoning)? ParseEstimate(string content)
    {
        var jsonSubstring = ExtractJsonObject(content);
        if (jsonSubstring is null) return null;

        try
        {
            using var doc = JsonDocument.Parse(jsonSubstring);
            var root = doc.RootElement;

            if (!root.TryGetProperty("estimatedValue", out var valueElement))
                return null;

            decimal? value = valueElement.ValueKind switch
            {
                JsonValueKind.Number when valueElement.TryGetDecimal(out var d) => d,
                JsonValueKind.String when decimal.TryParse(valueElement.GetString(), out var d) => d,
                _ => null
            };

            if (value is not decimal resolvedValue || resolvedValue < 0) return null;

            var reasoning = root.TryGetProperty("reasoning", out var reasoningElement)
                ? reasoningElement.GetString()
                : null;

            return (resolvedValue, reasoning);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? ExtractJsonObject(string content)
    {
        var text = content.Trim();
        if (text.StartsWith("```"))
        {
            var firstNewline = text.IndexOf('\n');
            if (firstNewline >= 0) text = text[(firstNewline + 1)..];
            var fenceEnd = text.LastIndexOf("```", StringComparison.Ordinal);
            if (fenceEnd >= 0) text = text[..fenceEnd];
        }

        var start = text.IndexOf('{');
        if (start < 0) return null;

        var depth = 0;
        for (var i = start; i < text.Length; i++)
        {
            if (text[i] == '{') depth++;
            else if (text[i] == '}')
            {
                depth--;
                if (depth == 0) return text[start..(i + 1)];
            }
        }

        return null;
    }
}
