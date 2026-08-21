using WatchTracker.Api.DTOs;

namespace WatchTracker.Api.Services;

public interface ICollectionProfileService
{
    Task<CollectionProfileDto> GetProfileAsync(int userId, CancellationToken ct = default);

    CandidateFitScoreDto ScoreCandidate(
        CollectionProfileDto profile,
        CollectionCandidateProfile candidate,
        decimal? budget = null);
}
