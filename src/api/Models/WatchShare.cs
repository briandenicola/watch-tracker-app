namespace WatchTracker.Api.Models;

/// <summary>
/// A public link to one watch. Anyone holding the token can see the watch's
/// spec sheet and photos without an account, so the token is the only thing
/// standing between the link and the world — it is 32 random bytes, and the
/// owner can revoke it at any time by deleting this row.
/// </summary>
public class WatchShare
{
    public int Id { get; set; }
    public int WatchId { get; set; }
    public Watch Watch { get; set; } = null!;
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    // Kept as issued rather than hashed, unlike API keys and refresh tokens:
    // the owner has to be able to come back tomorrow and copy the same link
    // again, which a one-way hash would make impossible. What it unlocks is a
    // read-only view of one watch, with no way back to the account.
    public required string Token { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastViewedAt { get; set; }
    public int ViewCount { get; set; }
}
