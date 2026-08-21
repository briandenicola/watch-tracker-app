namespace WatchTracker.Api.Models;

public class AdvisorMessage
{
    public int Id { get; set; }
    public int SessionId { get; set; }
    public AdvisorSession Session { get; set; } = null!;
    public AdvisorMessageRole Role { get; set; }
    public required string Content { get; set; }
    public string CitationsJson { get; set; } = "[]";
    public string RecommendationCardsJson { get; set; } = "[]";
    public string FollowUpsJson { get; set; } = "[]";
    public string ToolActivityJson { get; set; } = "[]";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
