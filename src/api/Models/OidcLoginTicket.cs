namespace WatchTracker.Api.Models;

public class OidcLoginTicket
{
    public int Id { get; set; }
    public required string CodeHash { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string? ReturnUrl { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ConsumedAt { get; set; }
    public bool IsActive => ConsumedAt is null && DateTime.UtcNow < ExpiresAt;
}
