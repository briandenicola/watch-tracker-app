using System.ComponentModel.DataAnnotations;

namespace WatchTracker.Api.DTOs;

public class WatchRecommendationRequestDto
{
    [Required, StringLength(100)]
    public required string Occasion { get; set; }

    [Required, StringLength(1500)]
    public required string OutfitDescription { get; set; }

    [StringLength(200)]
    public string? ColorPalette { get; set; }

    [StringLength(200)]
    public string? Weather { get; set; }

    [StringLength(500)]
    public string? Preferences { get; set; }
}

public class WatchRecommendationDto
{
    public required WatchRecommendationOptionDto Primary { get; set; }
    public required WatchRecommendationOptionDto Secondary { get; set; }
}

public class WatchRecommendationOptionDto
{
    public int WatchId { get; set; }
    public required string Brand { get; set; }
    public required string Model { get; set; }
    public string? ImageUrl { get; set; }
    public required string Reason { get; set; }
    public List<string> StylingTips { get; set; } = [];
}
