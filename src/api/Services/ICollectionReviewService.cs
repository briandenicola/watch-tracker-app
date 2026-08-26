using WatchTracker.Api.DTOs;

namespace WatchTracker.Api.Services;

public interface ICollectionReviewService
{
    /// <summary>
    /// Whether a review can be generated, and the stored one if there is one. The
    /// page needs both before it can offer the action, so they arrive together.
    /// </summary>
    Task<CollectionReviewStateDto> GetStateAsync(int userId, CancellationToken ct = default);

    /// <summary>Generates a review and replaces any stored one.</summary>
    Task<CollectionReviewStateDto> GenerateAsync(int userId, CancellationToken ct = default);
}
