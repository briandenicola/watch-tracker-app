using WatchTracker.Api.DTOs;

namespace WatchTracker.Api.Services;

public interface ICollectionProfileService
{
    Task<CollectionProfileDto> GetProfileAsync(int userId, CancellationToken ct = default);

    /// <summary>
    /// The counted facts behind a collection review: the same coverage and
    /// insight math as <see cref="GetProfileAsync"/>, run over the collection,
    /// the wish list, and the two combined.
    /// </summary>
    Task<CollectionReviewFactsDto> GetReviewFactsAsync(int userId, CancellationToken ct = default);

    CandidateFitScoreDto ScoreCandidate(
        CollectionProfileDto profile,
        CollectionCandidateProfile candidate,
        decimal? budget = null);
}
