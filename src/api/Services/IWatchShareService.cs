using WatchTracker.Api.DTOs;

namespace WatchTracker.Api.Services;

public interface IWatchShareService
{
    /// <summary>The watch's current share link, or null when it has none.</summary>
    Task<WatchShareDto?> GetAsync(int watchId, int userId, CancellationToken ct = default);

    /// <summary>
    /// The watch's share link, creating one the first time. Idempotent, so a
    /// second tap of Share hands back the link already in circulation rather
    /// than quietly orphaning it.
    /// </summary>
    Task<WatchShareDto?> CreateAsync(int watchId, int userId, CancellationToken ct = default);

    /// <summary>Kills the link. Anyone holding it gets a 404 from then on.</summary>
    Task<bool> RevokeAsync(int watchId, int userId, CancellationToken ct = default);

    /// <summary>
    /// The public view of a shared watch, or null when the token is unknown or
    /// has been revoked. Records the visit as a side effect.
    /// </summary>
    Task<SharedWatchDto?> ViewAsync(string token, CancellationToken ct = default);
}
