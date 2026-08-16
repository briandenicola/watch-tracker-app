using System.ComponentModel.DataAnnotations;
using WatchTracker.Api.Models;

namespace WatchTracker.Api.DTOs;

public class StyleRecommendationDto
{
    public int Id { get; set; }
    public int WatchId { get; set; }
    public string? Occasion { get; set; }
    public string? Weather { get; set; }
    public required string Summary { get; set; }
    public required string Outfit { get; set; }
    public bool? WasHelpful { get; set; }
    public string? FeedbackNotes { get; set; }
    public DateTime? FeedbackAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class StyleMessageDto
{
    public int Id { get; set; }
    public StyleMessageRole Role { get; set; }
    public required string Content { get; set; }
    public StyleRecommendationDto? Recommendation { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class StyleSessionDto
{
    public int Id { get; set; }
    public string? Occasion { get; set; }
    public string? Weather { get; set; }
    public List<StyleMessageDto> Messages { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Everything the chat needs to render after any turn: the transcript, the
/// agent's memory for this watch, and the questions it wants answered next.
/// </summary>
public class StyleChatStateDto
{
    public bool Configured { get; set; }
    public string? ConfigurationHint { get; set; }
    public required StyleSessionDto Session { get; set; }
    public List<StyleRecommendationDto> Memory { get; set; } = [];
    public List<string> FollowUps { get; set; } = [];
}

public class SendStyleMessageDto
{
    [StringLength(2000)]
    public string? Message { get; set; }

    [StringLength(200)]
    public string? Occasion { get; set; }

    [StringLength(200)]
    public string? Weather { get; set; }
}

public class StyleFeedbackDto
{
    public bool WasHelpful { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }
}
