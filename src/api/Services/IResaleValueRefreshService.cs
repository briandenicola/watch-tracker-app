using WatchTracker.Api.DTOs;

namespace WatchTracker.Api.Services;

public interface IResaleValueRefreshService
{
    Task<WatchDto?> RefreshWatchAsync(int watchId, int userId, CancellationToken ct = default);
    Task<int> RefreshDueWatchesAsync(CancellationToken ct = default);
    Task<ResaleRefreshSummaryDto> RefreshAllNowAsync(CancellationToken ct = default);
}
