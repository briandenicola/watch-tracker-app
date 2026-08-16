namespace WatchTracker.Api.Models;

/// <summary>
/// An outfit the style agent recommended, kept as the agent's long-term memory.
/// Every later conversation about the watch is primed with these — together with
/// whatever the user said about how they worked out — so the agent stops
/// repeating ideas that missed and leans into the ones that landed.
/// </summary>
public class StyleRecommendation
{
    public int Id { get; set; }
    public int WatchId { get; set; }
    public Watch Watch { get; set; } = null!;
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    // Null once the conversation it came from is gone — the memory outlives the chat.
    public int? SessionId { get; set; }
    public StyleSession? Session { get; set; }

    public string? Occasion { get; set; }
    public string? Weather { get; set; }

    /// <summary>One-line label for the outfit, used in the memory list.</summary>
    public required string Summary { get; set; }

    /// <summary>The full recommendation as markdown.</summary>
    public required string Outfit { get; set; }

    public bool? WasHelpful { get; set; }
    public string? FeedbackNotes { get; set; }
    public DateTime? FeedbackAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
