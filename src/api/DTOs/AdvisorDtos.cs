using System.ComponentModel.DataAnnotations;
using WatchTracker.Api.Models;

namespace WatchTracker.Api.DTOs;

public class AdvisorCitationDto
{
    public required string Title { get; set; }
    public required string Url { get; set; }
    public required string Provider { get; set; }
    public DateTime ObservedAt { get; set; }
}

public class AdvisorRecommendationCardDto
{
    public int? WatchId { get; set; }
    public string? Provider { get; set; }
    public string? ProviderItemId { get; set; }
    public required string Title { get; set; }
    public string? ItemUrl { get; set; }
    public string? ImageUrl { get; set; }
    public decimal? Price { get; set; }
    public string? Currency { get; set; }
    public string? Condition { get; set; }
    public DateTime? ObservedAt { get; set; }
    public int? FitScore { get; set; }
    public List<string> Reasons { get; set; } = [];
}

public class AdvisorToolActivityDto
{
    public required string Tool { get; set; }
    public required string Status { get; set; }
    public string? Message { get; set; }
}

public class AdvisorMessageDto
{
    public int Id { get; set; }
    public AdvisorMessageRole Role { get; set; }
    public required string Content { get; set; }
    public List<AdvisorCitationDto> Citations { get; set; } = [];
    public List<AdvisorRecommendationCardDto> RecommendationCards { get; set; } = [];
    public List<string> FollowUps { get; set; } = [];
    public List<AdvisorToolActivityDto> ToolActivity { get; set; } = [];
    public DateTime CreatedAt { get; set; }
}

public class AdvisorSessionDto
{
    public int Id { get; set; }
    public List<AdvisorMessageDto> Messages { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class AdvisorChatStateDto
{
    public bool Configured { get; set; }
    public string? ConfigurationHint { get; set; }
    public required AdvisorSessionDto Session { get; set; }
}

public class SendAdvisorMessageDto
{
    [Required, StringLength(2000, MinimumLength = 1)]
    public required string Message { get; set; }
}
