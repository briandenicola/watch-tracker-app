namespace WatchTracker.Api.Models;

/// <summary>
/// One conversation between a user and the style agent about a single watch.
/// Starting a new chat creates another session; the old one is kept so the
/// transcript — and the recommendations that came out of it — are not lost.
/// </summary>
public class StyleSession
{
    public int Id { get; set; }
    public int WatchId { get; set; }
    public Watch Watch { get; set; } = null!;
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    // The guidance the user gave most recently. Held on the session so it is
    // still in the agent's context on later turns without being restated.
    public string? Occasion { get; set; }
    public string? Weather { get; set; }

    public ICollection<StyleMessage> Messages { get; set; } = [];
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
