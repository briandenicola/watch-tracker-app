using WatchTracker.Api.DTOs;

namespace WatchTracker.Api.Services;

public interface ICollectionReviewCandidateService
{
    /// <summary>
    /// Finds buyable watches that fill the gaps the stored review identified, and
    /// replaces any candidates already stored against it.
    /// </summary>
    Task<CollectionReviewCandidatesDto> GenerateAsync(
        int userId,
        GenerateCandidatesDto request,
        CancellationToken ct = default);

    /// <summary>
    /// Adds one stored candidate to the wish list, or reports the watch that
    /// already covers it. Null when the candidate is not among the stored ones.
    /// </summary>
    Task<AdvisorWishlistActionResultDto?> AddToWishlistAsync(
        int userId,
        CandidateWishlistActionDto request,
        CancellationToken ct = default);
}
