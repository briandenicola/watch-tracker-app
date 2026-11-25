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

    /// <summary>
    /// The same, for a watch named by hand: brand and model are enough and a link
    /// is optional. Used where a person (or their assistant) says what they want
    /// rather than picking a listing.
    /// </summary>
    Task<AdvisorWishlistActionResultDto> AddWatchAsync(
        AdvisorRecommendationCardDto card,
        int userId,
        string note,
        CancellationToken ct = default);
}
