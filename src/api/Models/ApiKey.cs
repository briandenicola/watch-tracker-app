namespace WatchTracker.Api.Models;

public class ApiKey
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public required string Name { get; set; }
    public required string KeyHash { get; set; }
    /// <summary>Canonical comma-separated MCP scopes: "read" or "read,agents".</summary>
    public string Scopes { get; set; } = "read";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastUsedAt { get; set; }
}
