using WatchTracker.Api.Models;

namespace WatchTracker.Api.Services;

public record ResaleEstimateResult(decimal Value, string? Reasoning, string SourceName);

public interface IResaleValueEstimator
{
    Task<ResaleEstimateResult?> EstimateAsync(Watch watch, CancellationToken ct = default);
}
