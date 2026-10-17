using System.Text.Json;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace WatchTracker.Api.Services;

public sealed class WatchPatrolMcpClient(
    IWatchPatrolConnectionService connections,
    IHttpClientFactory clients) : IWatchPatrolMcpClient, IAsyncDisposable
{
    private McpClient? session;
    private HttpClientTransport? transport;
    private int? sessionUserId;

    public async Task<IReadOnlyList<Tool>> ListToolsAsync(int userId, CancellationToken ct)
    {
        var client = await GetSessionAsync(userId, ct);
        var result = await client.ListToolsAsync(new ListToolsRequestParams(), ct);
        if (result.NextCursor is not null || result.Tools.Count > 50)
            throw new InvalidOperationException("WatchPatrol returned more tools than the Advisor supports.");
        return result.Tools.ToList();
    }

    public async Task<CallToolResult> CallToolAsync(
        int userId, string name, JsonElement arguments, CancellationToken ct)
    {
        var client = await GetSessionAsync(userId, ct);
        return await client.CallToolAsync(new CallToolRequestParams
        {
            Name = name,
            Arguments = arguments.EnumerateObject().ToDictionary(
                property => property.Name, property => property.Value.Clone())
        }, ct);
    }

    private async Task<McpClient> GetSessionAsync(int userId, CancellationToken ct)
    {
        if (sessionUserId is not null && sessionUserId != userId)
            throw new InvalidOperationException("A WatchPatrol session cannot be shared between users.");
        if (session is not null) return session;
        var token = await connections.GetAccessTokenAsync(userId, ct);
        transport = new HttpClientTransport(new HttpClientTransportOptions
        {
            Name = "WatchPatrol",
            Endpoint = new Uri(WatchPatrolConnectionService.Resource),
            TransportMode = HttpTransportMode.StreamableHttp,
            EnableStandaloneGetStream = false,
            ConnectionTimeout = TimeSpan.FromSeconds(20),
            MaxReconnectionAttempts = 0,
            AdditionalHeaders = new Dictionary<string, string>
            {
                ["Authorization"] = $"Bearer {token}"
            }
        }, clients.CreateClient("WatchPatrol"), ownsHttpClient: true);
        session = await McpClient.CreateAsync(transport, cancellationToken: ct);
        sessionUserId = userId;
        return session;
    }

    public async ValueTask DisposeAsync()
    {
        if (session is not null) await session.DisposeAsync();
        if (transport is not null) await transport.DisposeAsync();
    }
}
