namespace WatchTracker.Api.Models;

/// <summary>A single turn in a <see cref="StyleSession"/> transcript.</summary>
public class StyleMessage
{
    public int Id { get; set; }
    public int SessionId { get; set; }
    public StyleSession Session { get; set; } = null!;
    public StyleMessageRole Role { get; set; }
    public required string Content { get; set; }

    // Set on the assistant turn that produced a stored recommendation, so the
    // chat can show the feedback controls next to the message that earned them.
    public int? RecommendationId { get; set; }
    public StyleRecommendation? Recommendation { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
