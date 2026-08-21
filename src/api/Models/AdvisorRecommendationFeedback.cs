namespace WatchTracker.Api.Models;

public class AdvisorRecommendationFeedback
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int MessageId { get; set; }
    public AdvisorMessage Message { get; set; } = null!;
    public required string Provider { get; set; }
    public required string ProviderItemId { get; set; }
    public required string Title { get; set; }
    public AdvisorFeedbackKind Kind { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
