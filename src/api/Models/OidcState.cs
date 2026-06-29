namespace WatchTracker.Api.Models;

public class OidcState
{
    public int Id { get; set; }
    public required string StateHash { get; set; }
    public OidcProvider Provider { get; set; }
    public required string CodeVerifierProtected { get; set; }
    public required string NonceHash { get; set; }
    public string? ReturnUrl { get; set; }
    public int? LinkUserId { get; set; }
    public User? LinkUser { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ConsumedAt { get; set; }
    public bool IsActive => ConsumedAt is null && DateTime.UtcNow < ExpiresAt;
}
