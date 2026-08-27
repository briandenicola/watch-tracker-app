using WatchTracker.Api.DTOs;
using WatchTracker.Api.Models;

namespace WatchTracker.Api.Services;

public interface IPriceAlertEvaluator
{
    Task<int> EvaluateAsync(PriceObservation observation, Watch watch, CancellationToken ct = default);
}

public interface IPriceAlertService
{
    Task<IReadOnlyList<PriceAlertDto>> GetAlertsAsync(
        int userId,
        bool unreadOnly,
        CancellationToken ct = default);
    Task<bool> MarkReadAsync(int alertId, int userId, CancellationToken ct = default);
    Task<int> MarkAllReadAsync(int userId, CancellationToken ct = default);
}
