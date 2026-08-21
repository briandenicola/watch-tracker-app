using System.ComponentModel.DataAnnotations;
using WatchTracker.Api.Models;

namespace WatchTracker.Api.DTOs;

public class AdvisorCitationDto
{
    public required string Title { get; set; }
    public required string Url { get; set; }
    public required string Provider { get; set; }
    public string Confidence { get; set; } = "medium";
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
    public decimal? ShippingPrice { get; set; }
    public decimal? TotalPrice { get; set; }
    public string? Currency { get; set; }
    public string? Condition { get; set; }
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public string? ReferenceNumber { get; set; }
    public DateTime? ObservedAt { get; set; }
    public int? FitScore { get; set; }
    public List<string> Reasons { get; set; } = [];
    public AdvisorRecommendationFeedbackDto? Feedback { get; set; }
}

public class AdvisorRecommendationFeedbackDto
{
    public int Id { get; set; }
    public AdvisorFeedbackKind Kind { get; set; }
    public string? Notes { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class SaveAdvisorFeedbackDto
{
    [Required, StringLength(50)]
    public required string Provider { get; set; }

    [Required, StringLength(200)]
    public required string ProviderItemId { get; set; }

    [EnumDataType(typeof(AdvisorFeedbackKind))]
    public AdvisorFeedbackKind Kind { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }
}

public class AdvisorRecommendationActionDto
{
    [Required, StringLength(50)]
    public required string Provider { get; set; }

    [Required, StringLength(200)]
    public required string ProviderItemId { get; set; }
}

public class AdvisorWishlistActionResultDto
{
    public bool Added { get; set; }
    public int WatchId { get; set; }
    public required string Message { get; set; }
}

public class AdvisorFeedbackMemoryDto
{
    public required string Provider { get; set; }
    public required string Title { get; set; }
    public AdvisorFeedbackKind Kind { get; set; }
    public string? Notes { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class AdvisorToolActivityDto
{
    public required string Tool { get; set; }
    public required string Status { get; set; }
    public string? Message { get; set; }
    public long DurationMs { get; set; }
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
