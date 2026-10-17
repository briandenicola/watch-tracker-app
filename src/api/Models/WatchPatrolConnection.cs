namespace WatchTracker.Api.Models;

public class WatchPatrolConnection
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string? CredentialsProtected { get; set; }
    public string? PendingStateHash { get; set; }
    public string? PendingAuthorizationProtected { get; set; }
    public DateTime? PendingExpiresAt { get; set; }
}
