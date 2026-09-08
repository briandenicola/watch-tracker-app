using WatchTracker.Api.DTOs;

namespace WatchTracker.Api.Services;

public interface IWishlistShareService
{
    /// <summary>The user's current wish list link, or null when they have none.</summary>
    Task<WishlistShareDto?> GetAsync(int userId, CancellationToken ct = default);

    /// <summary>
    /// The user's wish list link, creating one the first time. Idempotent, so a
    /// second tap of Share hands back the link already in circulation.
    /// </summary>
    Task<WishlistShareDto> CreateAsync(int userId, UpdateWishlistShareDto options, CancellationToken ct = default);

    /// <summary>Changes what the link exposes without reissuing it.</summary>
    Task<WishlistShareDto?> UpdateAsync(int userId, UpdateWishlistShareDto options, CancellationToken ct = default);

    /// <summary>Kills the link. Anyone holding it gets a 404 from then on.</summary>
    Task<bool> RevokeAsync(int userId, CancellationToken ct = default);

    /// <summary>
    /// The public view of a shared wish list, or null when the token is unknown
    /// or revoked. Records the visit as a side effect.
    /// </summary>
    Task<SharedWishlistDto?> ViewAsync(string token, CancellationToken ct = default);

    Task<IReadOnlyList<WishlistShareUserDto>> SearchUsersAsync(
        int ownerUserId, string query, CancellationToken ct = default);
    Task<IReadOnlyList<WishlistUserShareDto>> GetUserSharesAsync(
        int ownerUserId, CancellationToken ct = default);
    Task<WishlistUserShareDto?> ShareWithUserAsync(
        int ownerUserId, CreateWishlistUserShareDto request, CancellationToken ct = default);
    Task<bool> RevokeUserShareAsync(
        int ownerUserId, int shareId, CancellationToken ct = default);
    Task<IReadOnlyList<ReceivedWishlistShareDto>> GetReceivedSharesAsync(
        int recipientUserId, CancellationToken ct = default);
    Task<SharedWishlistDto?> ViewReceivedShareAsync(
        int shareId, int recipientUserId, CancellationToken ct = default);
}
