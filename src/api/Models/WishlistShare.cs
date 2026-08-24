namespace WatchTracker.Api.Models;

/// <summary>
/// A public link to a user's whole wish list — the list people actually ask
/// for. Like <see cref="WatchShare"/>, the token is the only way in and the
/// owner can revoke it by deleting this row.
/// </summary>
public class WishlistShare
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    // Stored as issued rather than hashed, for the same reason as WatchShare:
    // the owner has to be able to come back and copy the same link again.
    public required string Token { get; set; }

    /// <summary>
    /// Whether visitors see each item's target price. Off unless the owner says
    /// otherwise — useful on a list you are hinting with, but nobody's budget
    /// should become public because they did not notice a checkbox.
    /// </summary>
    public bool IncludePrices { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastViewedAt { get; set; }
    public int ViewCount { get; set; }
}
