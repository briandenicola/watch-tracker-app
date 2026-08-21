using WatchTracker.Api.Models;

namespace WatchTracker.Api.DTOs;

public enum CollectionInsightConfidence
{
    Low,
    Medium,
    High
}

public class CollectionCoverageDto
{
    public required string Dimension { get; set; }
    public List<CollectionCoverageValueDto> Values { get; set; } = [];
}

public class CollectionCoverageValueDto
{
    public required string Value { get; set; }
    public int Count { get; set; }
    public List<int> WatchIds { get; set; } = [];
}

public class CollectionInsightDto
{
    public required string Summary { get; set; }
    public required string Reason { get; set; }
    public CollectionInsightConfidence Confidence { get; set; }
    public List<int> WatchIds { get; set; } = [];
    public List<string> EvidenceFields { get; set; } = [];
}

public class WishlistOverlapDto
{
    public int WishlistWatchId { get; set; }
    public List<int> CollectionWatchIds { get; set; } = [];
    public required string Reason { get; set; }
}

public class CollectionProfileDto
{
    public int ActiveWatchCount { get; set; }
    public int WishlistWatchCount { get; set; }
    public int DataCompletenessPercent { get; set; }
    public List<CollectionCoverageDto> Coverage { get; set; } = [];
    public List<CollectionInsightDto> Gaps { get; set; } = [];
    public List<CollectionInsightDto> Redundancies { get; set; } = [];
    public List<CollectionInsightDto> DataQuality { get; set; } = [];
    public List<int> UnderusedWatchIds { get; set; } = [];
    public List<int> StaleResaleValueWatchIds { get; set; } = [];
    public List<WishlistOverlapDto> WishlistOverlaps { get; set; } = [];
}

public class CollectionCandidateProfile
{
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public MovementType? MovementType { get; set; }
    public double? CaseSizeMm { get; set; }
    public string? DialColor { get; set; }
    public string? BandType { get; set; }
    public decimal? Price { get; set; }
}

public class CandidateFitScoreDto
{
    public int TotalScore { get; set; }
    public int CollectionFitScore { get; set; }
    public int? BudgetFitScore { get; set; }
    public int EvidenceConfidencePercent { get; set; }
    public List<string> Reasons { get; set; } = [];
}
