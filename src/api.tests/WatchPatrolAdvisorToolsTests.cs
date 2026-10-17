using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol.Protocol;
using WatchTracker.Api.DTOs;
using WatchTracker.Api.Services;

namespace WatchTracker.Api.Tests;

public class WatchPatrolAdvisorToolsTests
{
    [Theory]
    [InlineData("get_pricing", true, false, true)]
    [InlineData("search_listings", true, false, true)]
    [InlineData("get_seller", true, false, true)]
    [InlineData("market_summary", true, false, true)]
    [InlineData("price_movers", true, false, true)]
    [InlineData("search_listings", false, false, false)]
    [InlineData("search_listings", null, false, false)]
    [InlineData("get_pricing", true, true, false)]
    [InlineData("create_alert", true, false, false)]
    [InlineData("star_listing", true, false, false)]
    [InlineData("get_listing_image", true, false, false)]
    [InlineData("identify_watch", true, false, false)]
    [InlineData("get_bulk_prices", true, false, false)]
    [InlineData("get_account", true, false, false)]
    public void Only_explicitly_read_only_research_tools_are_allowed(
        string name, bool? readOnly, bool destructive, bool expected)
    {
        Assert.Equal(expected, WatchPatrolAdvisorTools.IsAllowed(new Tool
        {
            Name = name,
            Annotations = new() { ReadOnlyHint = readOnly, DestructiveHint = destructive }
        }));
    }

    [Fact]
    public async Task Discovery_and_calls_use_current_user_and_preserve_verbatim_evidence()
    {
        var client = new StubClient();
        var tools = Create(client);
        var context = Context();

        var discovery = await tools.ExecuteAsync("watchpatrol_tools", Json(new { }), context, default);
        Assert.Contains("search_listings", discovery.OutputJson);
        Assert.DoesNotContain("create_alert", discovery.OutputJson);
        Assert.Empty(context.Sources);
        var result = await tools.ExecuteAsync("watchpatrol_call",
            Json(new { name = "search_listings", arguments = new { query = "Seiko", limit = 2 } }),
            context, default);

        Assert.Equal(7, client.LastUserId);
        Assert.Equal("search_listings", client.LastName);
        Assert.Equal("Seiko", client.LastArguments.GetProperty("query").GetString());
        Assert.Contains("1250", result.OutputJson);
        Assert.Contains("1250", result.Activity.Evidence);
        Assert.Equal("completed", result.Activity.Status);
        Assert.Contains("https://www.watchpatrol.net/listing/123/", context.Sources.Keys);
        Assert.Empty(context.Listings);
        Assert.Empty(context.ListingScores);
    }

    [Fact]
    public async Task Unknown_write_and_undiscovered_tools_never_reach_remote_server()
    {
        var client = new StubClient();
        var tools = Create(client);
        var context = Context();
        var beforeDiscovery = await tools.ExecuteAsync("watchpatrol_call",
            Json(new { name = "search_listings", arguments = new { } }), context, default);
        Assert.Equal("failed", beforeDiscovery.Activity.Status);
        await tools.ExecuteAsync("watchpatrol_tools", Json(new { }), context, default);
        var write = await tools.ExecuteAsync("watchpatrol_call",
            Json(new { name = "create_alert", arguments = new { } }), context, default);
        var unknown = await tools.ExecuteAsync("watchpatrol_call",
            Json(new { name = "invented_tool", arguments = new { } }), context, default);

        Assert.Equal("failed", write.Activity.Status);
        Assert.Equal("failed", unknown.Activity.Status);
        Assert.Equal(0, client.Calls);
        Assert.Empty(context.Sources);
    }

    [Fact]
    public async Task Provider_error_and_oversized_results_never_become_evidence()
    {
        var client = new StubClient { Error = true };
        var tools = Create(client);
        var context = Context();
        await tools.ExecuteAsync("watchpatrol_tools", Json(new { }), context, default);
        var input = Json(new { name = "search_listings", arguments = new { } });

        var error = await tools.ExecuteAsync("watchpatrol_call", input, context, default);
        Assert.Equal("failed", error.Activity.Status);
        Assert.Empty(context.Sources);
        Assert.Null(error.Activity.Evidence);
        client.Error = false;
        client.Text = new string('x', 8_001);
        var oversized = await tools.ExecuteAsync("watchpatrol_call", input, context, default);
        Assert.Equal("failed", oversized.Activity.Status);
        Assert.Empty(context.Sources);
        Assert.Null(oversized.Activity.Evidence);
    }

    [Fact]
    public async Task Rate_limit_is_explicit_and_does_not_trigger_retries()
    {
        var client = new StubClient { RateLimited = true };
        var result = await Create(client).ExecuteAsync(
            "watchpatrol_tools", Json(new { }), Context(), default);
        Assert.Equal("failed", result.Activity.Status);
        Assert.Contains("usage limit", result.Activity.Message);
        Assert.Equal(0, client.Calls);
    }

    [Fact]
    public async Task Structured_aggregate_results_are_preserved_without_becoming_listing_cards()
    {
        var client = new StubClient
        {
            Structured = Json(new { median = 1250, currency = "USD", sampleSize = 30 })
        };
        var tools = Create(client);
        var context = Context();
        await tools.ExecuteAsync("watchpatrol_tools", Json(new { }), context, default);
        var result = await tools.ExecuteAsync("watchpatrol_call",
            Json(new { name = "search_listings", arguments = new { } }), context, default);
        Assert.Equal("completed", result.Activity.Status);
        Assert.Contains("\"median\": 1250", result.Activity.Evidence);
        Assert.Contains(WatchPatrolConnectionService.Origin + "/", context.Sources.Keys);
        Assert.Empty(context.Listings);
    }

    [Fact]
    public async Task Provider_timeout_is_a_visible_failure_but_request_cancellation_propagates()
    {
        var client = new StubClient { Cancelled = true };
        var tools = Create(client);
        var result = await tools.ExecuteAsync("watchpatrol_tools", Json(new { }), Context(), default);
        Assert.Equal("failed", result.Activity.Status);
        Assert.Contains("timed out", result.Activity.Message);
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            tools.ExecuteAsync("watchpatrol_tools", Json(new { }), Context(), cancelled.Token));
    }

    private static WatchPatrolAdvisorTools Create(StubClient client) =>
        new(client, NullLogger<WatchPatrolAdvisorTools>.Instance);
    private static AdvisorToolContext Context() => new(7, new CollectionProfileDto());
    private static JsonElement Json(object value) => JsonSerializer.SerializeToElement(value);

    private sealed class StubClient : IWatchPatrolMcpClient
    {
        public int Calls { get; private set; }
        public int LastUserId { get; private set; }
        public string? LastName { get; private set; }
        public JsonElement LastArguments { get; private set; }
        public bool Error { get; set; }
        public bool RateLimited { get; init; }
        public bool Cancelled { get; init; }
        public JsonElement? Structured { get; init; }
        public string Text { get; set; } = """{"median":1250,"currency":"USD","url":"https://www.watchpatrol.net/listing/123/"}""";

        public Task<IReadOnlyList<Tool>> ListToolsAsync(int userId, CancellationToken ct)
        {
            LastUserId = userId;
            if (Cancelled) throw new OperationCanceledException(ct);
            if (RateLimited)
                throw new HttpRequestException("private-provider-message", null, HttpStatusCode.TooManyRequests);
            return Task.FromResult<IReadOnlyList<Tool>>(
            [
                new()
                {
                    Name = "search_listings",
                    InputSchema = Json(new { type = "object" }),
                    Annotations = new() { ReadOnlyHint = true }
                },
                new()
                {
                    Name = "create_alert",
                    Annotations = new() { ReadOnlyHint = false }
                }
            ]);
        }

        public Task<CallToolResult> CallToolAsync(
            int userId, string name, JsonElement arguments, CancellationToken ct)
        {
            Calls++;
            LastUserId = userId;
            LastName = name;
            LastArguments = arguments;
            return Task.FromResult(new CallToolResult
            {
                IsError = Error,
                StructuredContent = Structured,
                Content = [new TextContentBlock { Text = Text }]
            });
        }
    }
}
