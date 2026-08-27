using WatchTracker.Api.DTOs;

namespace WatchTracker.Api.Services;

public enum PriceScanStatus
{
    Found,
    NotConfigured,
    Blocked,
    ProviderError,
    NoMatch
}

public interface IWishlistPriceScanner
{
    Task<PriceScanResultDto?> ScanAsync(int watchId, int userId, CancellationToken ct = default);
    Task<int> ScanDueAsync(CancellationToken ct = default);
    Task<IReadOnlyList<PriceObservationDto>?> GetObservationsAsync(
        int watchId,
        int userId,
        CancellationToken ct = default);
    Task<PriceMonitoringDto?> UpdateMonitoringAsync(
        int watchId,
        int userId,
        UpdatePriceMonitoringDto dto,
        CancellationToken ct = default);
}
