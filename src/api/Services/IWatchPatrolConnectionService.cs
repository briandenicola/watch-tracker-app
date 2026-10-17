using WatchTracker.Api.DTOs;

namespace WatchTracker.Api.Services;

public interface IWatchPatrolConnectionService
{
    Task<WatchPatrolStatusDto> GetStatusAsync(int userId, CancellationToken ct = default);
    Task<string> ConnectAsync(int userId, string redirectUri, CancellationToken ct = default);
    Task CompleteAsync(int userId, string code, string state, CancellationToken ct = default);
    Task DisconnectAsync(int userId, CancellationToken ct = default);
    Task<string> GetAccessTokenAsync(int userId, CancellationToken ct = default);
}
