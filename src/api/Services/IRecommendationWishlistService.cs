using WatchTracker.Api.DTOs;

namespace WatchTracker.Api.Services;

public interface IRecommendationWishlistService
{
    /// <summary>
    /// Adds a recommendation card to a user's wish list, or reports the wish list
    /// watch that already covers it. Returns null when the card carries no link to
    /// add. Where the card came from is the caller's business: it supplies the
    /// note recorded on the watch.
    /// </summary>
    Task<AdvisorWishlistActionResultDto?> AddAsync(
        AdvisorRecommendationCardDto card,
        int userId,
        string note,
        CancellationToken ct = default);
}
