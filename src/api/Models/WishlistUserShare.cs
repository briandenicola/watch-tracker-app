namespace WatchTracker.Api.Models;

public class WishlistUserShare
{
    public int Id { get; set; }
    public int OwnerUserId { get; set; }
    public User OwnerUser { get; set; } = null!;
    public int RecipientUserId { get; set; }
    public User RecipientUser { get; set; } = null!;
    public bool IncludePrices { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastViewedAt { get; set; }
    public int ViewCount { get; set; }
}
