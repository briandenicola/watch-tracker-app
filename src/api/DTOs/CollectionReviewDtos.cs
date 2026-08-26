using System.ComponentModel.DataAnnotations;

namespace WatchTracker.Api.DTOs;

/// <summary>
/// Coverage and insight statistics for one set of watches — the active
/// collection, the wish list, or the two together.
/// </summary>
public class CollectionSetStatsDto
{
    public required string Label { get; set; }
    public int WatchCount { get; set; }
    public int DataCompletenessPercent { get; set; }
    public List<CollectionCoverageDto> Coverage { get; set; } = [];
    public List<CollectionInsightDto> Redundancies { get; set; } = [];
    public List<CollectionInsightDto> Gaps { get; set; } = [];
}

/// <summary>One watch, flattened to the fields the review reasons about.</summary>
public class ReviewWatchDto
{
    public int Id { get; set; }
    public required string Brand { get; set; }
    public required string Model { get; set; }
    public required string MovementType { get; set; }
    public double? CaseSizeMm { get; set; }
    public string? DialColor { get; set; }
    public string? BandType { get; set; }
    public decimal? Price { get; set; }
    public int? WishlistPriority { get; set; }
    public int? TimesWorn { get; set; }
    public DateTime? LastWornDate { get; set; }
}

/// <summary>How well one wish list watch fills a gap in the current collection.</summary>
public class WishlistFitDto
{
    public int WatchId { get; set; }
    public int TotalScore { get; set; }
    public int CollectionFitScore { get; set; }
    public List<string> Reasons { get; set; } = [];
}

/// <summary>
/// Everything about the collection and wish list that is counted rather than
/// judged. The model is handed these and told not to recalculate them, so the
/// numbers in a review are always the database's, never the model's.
/// </summary>
public class CollectionReviewFactsDto
{
    public CollectionSetStatsDto Collection { get; set; } = new() { Label = "Collection" };
    public CollectionSetStatsDto Wishlist { get; set; } = new() { Label = "Wish list" };
    public CollectionSetStatsDto Combined { get; set; } = new() { Label = "Combined" };
    public List<CollectionInsightDto> DataQuality { get; set; } = [];
    public List<WishlistOverlapDto> WishlistOverlaps { get; set; } = [];
    public List<WishlistFitDto> WishlistFit { get; set; } = [];
    public List<ReviewWatchDto> CollectionWatches { get; set; } = [];
    public List<ReviewWatchDto> WishlistWatches { get; set; } = [];
    public List<int> UnderusedWatchIds { get; set; } = [];
}

/// <summary>One point the review makes, tied to the watches it is about.</summary>
public class CollectionReviewFindingDto
{
    public required string Summary { get; set; }
    public required string Detail { get; set; }
    public List<int> WatchIds { get; set; } = [];
}

/// <summary>
/// What the review page needs on load: whether a review can be generated at all,
/// and the stored one if there is one.
/// </summary>
public class CollectionReviewStateDto
{
    public bool Configured { get; set; }
    public string? ConfigurationHint { get; set; }
    public CollectionReviewDto? Review { get; set; }
}

/// <summary>How one marketplace answered when candidates were last generated.</summary>
public class MarketplaceProviderStatusDto
{
    public required string Provider { get; set; }
    public required string Status { get; set; }
    public string? Error { get; set; }
}

public class CollectionReviewCandidatesDto
{
    public List<AdvisorRecommendationCardDto> Candidates { get; set; } = [];
    public List<MarketplaceProviderStatusDto> MarketplaceStatus { get; set; } = [];
    public DateTime? GeneratedAt { get; set; }

    /// <summary>True when listings were dropped for being older than the freshness window.</summary>
    public bool DroppedStaleListings { get; set; }
}

public class GenerateCandidatesDto
{
    [Range(1, 10_000_000)]
    public decimal? Budget { get; set; }

    [StringLength(3, MinimumLength = 3)]
    public string? Currency { get; set; }
}

public class CandidateWishlistActionDto
{
    [Required, StringLength(50)]
    public required string Provider { get; set; }

    [Required, StringLength(200)]
    public required string ProviderItemId { get; set; }
}

public class CollectionReviewDto
{
    public string? Summary { get; set; }
    public List<CollectionReviewFindingDto> Strengths { get; set; } = [];
    public List<CollectionReviewFindingDto> Weaknesses { get; set; } = [];
    public List<CollectionReviewFindingDto> Recommendations { get; set; } = [];
    public CollectionReviewFactsDto Facts { get; set; } = new();
    public DateTime GeneratedAt { get; set; }

    /// <summary>True once watches have been added, removed, or edited since this ran.</summary>
    public bool IsStale { get; set; }

    public CollectionReviewCandidatesDto Candidates { get; set; } = new();
}
