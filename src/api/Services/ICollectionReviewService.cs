using WatchTracker.Api.DTOs;

namespace WatchTracker.Api.Services;

public interface ICollectionReviewService
{
    /// <summary>The stored review, or null when one has never been generated.</summary>
    Task<CollectionReviewDto?> GetLatestAsync(int userId, CancellationToken ct = default);

    /// <summary>Generates a review and replaces any stored one.</summary>
    Task<CollectionReviewDto> GenerateAsync(int userId, CancellationToken ct = default);
}
