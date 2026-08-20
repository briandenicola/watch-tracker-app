using WatchTracker.Api.DTOs;

namespace WatchTracker.Api.Services;

public interface IWatchRecommendationService
{
    Task<WatchRecommendationDto> RecommendAsync(
        WatchRecommendationRequestDto request,
        int userId,
        CancellationToken ct = default);
}
