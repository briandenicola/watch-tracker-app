using WatchTracker.Api.DTOs;

namespace WatchTracker.Api.Services;

public interface IWatchDispositionService
{
    Task<WatchDto?> RetireAsync(int id, int userId, CancellationToken ct = default);
    Task<WatchDto?> UnretireAsync(int id, int userId, CancellationToken ct = default);
    Task<WatchDto?> SetDispositionAsync(int id, int userId, UpdateWatchDispositionDto dto, CancellationToken ct = default);
    Task<WatchDto?> ClearDispositionAsync(int id, int userId, CancellationToken ct = default);
}
