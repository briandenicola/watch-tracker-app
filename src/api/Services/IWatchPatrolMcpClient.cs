using System.Text.Json;
using ModelContextProtocol.Protocol;

namespace WatchTracker.Api.Services;

public interface IWatchPatrolMcpClient
{
    Task<IReadOnlyList<Tool>> ListToolsAsync(int userId, CancellationToken ct);
    Task<CallToolResult> CallToolAsync(
        int userId, string name, JsonElement arguments, CancellationToken ct);
}
