using System.Net;
using System.Text;
using System.Text.Json;
using ModelContextProtocol.Protocol;
using WatchTracker.Api.DTOs;
using WatchTracker.Api.Services;

namespace WatchTracker.Api.Tests;

public class WatchPatrolMcpClientTests
{
    [Fact]
    public async Task Sdk_handshake_discovery_and_call_use_only_the_connected_users_bearer_token()
    {
        var handler = new McpHandler();
        var connections = new Connections();
        await using var client = new WatchPatrolMcpClient(connections, new Factory(handler));
        var tools = await client.ListToolsAsync(7, default);
        Assert.Equal("get_pricing", Assert.Single(tools).Name);
        var result = await client.CallToolAsync(7, "get_pricing",
            JsonSerializer.SerializeToElement(new { model = "example" }), default);

        Assert.Equal(7, connections.RequestedUserId);
        Assert.Single(result.Content);
        Assert.Equal("example", handler.Arguments.GetProperty("model").GetString());
        Assert.Contains("initialize", handler.Methods);
        Assert.Contains("notifications/initialized", handler.Methods);
        Assert.Contains("tools/list", handler.Methods);
        Assert.Contains("tools/call", handler.Methods);
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.ListToolsAsync(8, default));
    }

    [Fact]
    public async Task Provider_unauthorized_response_is_not_retried_or_treated_as_a_result()
    {
        var handler = new McpHandler { Unauthorized = true };
        await using var client = new WatchPatrolMcpClient(new Connections(), new Factory(handler));
        await Assert.ThrowsAsync<HttpRequestException>(() => client.ListToolsAsync(7, default));
        Assert.Equal(1, handler.Requests);
    }

    private sealed class Factory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private sealed class Connections : IWatchPatrolConnectionService
    {
        public int? RequestedUserId { get; private set; }
        public Task<string> GetAccessTokenAsync(int userId, CancellationToken ct = default)
        {
            RequestedUserId = userId;
            return Task.FromResult("user-specific-test-token");
        }
        public Task<WatchPatrolStatusDto> GetStatusAsync(int userId, CancellationToken ct = default) =>
            throw new NotSupportedException();
        public Task<string> ConnectAsync(int userId, string redirectUri, CancellationToken ct = default) =>
            throw new NotSupportedException();
        public Task CompleteAsync(int userId, string code, string state, CancellationToken ct = default) =>
            throw new NotSupportedException();
        public Task DisconnectAsync(int userId, CancellationToken ct = default) =>
            throw new NotSupportedException();
    }

    private sealed class McpHandler : HttpMessageHandler
    {
        public int Requests { get; private set; }
        public bool Unauthorized { get; init; }
        public List<string> Methods { get; } = [];
        public JsonElement Arguments { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Requests++;
            Assert.Equal(WatchPatrolConnectionService.Resource, request.RequestUri!.AbsoluteUri);
            Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
            Assert.Equal("user-specific-test-token", request.Headers.Authorization?.Parameter);
            if (Unauthorized) return new(HttpStatusCode.Unauthorized);
            if (request.Method == HttpMethod.Delete) return new(HttpStatusCode.NoContent);
            Assert.Equal(HttpMethod.Post, request.Method);
            using var document = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(ct));
            var root = document.RootElement;
            var method = root.GetProperty("method").GetString()!;
            Methods.Add(method);
            if (!root.TryGetProperty("id", out var id))
                return new(HttpStatusCode.Accepted);
            if (method == "server/discover")
                return new(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(new
                    {
                        jsonrpc = "2.0", id,
                        error = new { code = -32601, message = "Method not found" }
                    }), Encoding.UTF8, "application/json")
                };
            object result = method switch
            {
                "initialize" => new
                {
                    protocolVersion = root.GetProperty("params").GetProperty("protocolVersion").GetString(),
                    capabilities = new { tools = new { } },
                    serverInfo = new { name = "WatchPatrol test", version = "1" }
                },
                "tools/list" => new
                {
                    tools = new[] { new Tool { Name = "get_pricing", Annotations = new() { ReadOnlyHint = true } } }
                },
                "tools/call" => ToolResult(root.GetProperty("params")),
                _ => throw new InvalidOperationException($"Unexpected MCP method: {method}")
            };
            return new(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(new
                {
                    jsonrpc = "2.0", id, result
                }), Encoding.UTF8, "application/json")
            };
        }

        private CallToolResult ToolResult(JsonElement input)
        {
            Assert.Equal("get_pricing", input.GetProperty("name").GetString());
            Arguments = input.GetProperty("arguments").Clone();
            return new() { Content = [new TextContentBlock { Text = """{"median":1250,"currency":"USD"}""" }] };
        }
    }
}
