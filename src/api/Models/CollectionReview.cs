namespace WatchTracker.Api.Models;

/// <summary>
/// The latest review of a user's collection and wish list. Generating one is the
/// most expensive call in the app, so the result is kept and re-read rather than
/// regenerated on every visit. One row per user, replaced on each run.
/// </summary>
public class CollectionReview
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public string? Summary { get; set; }
    public required string StrengthsJson { get; set; }
    public required string WeaknessesJson { get; set; }
    public required string RecommendationsJson { get; set; }

    /// <summary>The counted facts the model was given, kept so the report renders without recomputing.</summary>
    public required string FactsJson { get; set; }

    // Fingerprint of the watches this review was based on. Compared against the
    // current collection to tell the reader when the review has gone stale.
    public int CollectionWatchCount { get; set; }
    public int WishlistWatchCount { get; set; }
    public DateTime? WatchesUpdatedAt { get; set; }

    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    // Gap-filling purchase candidates, generated separately from the report so a
    // regenerated review does not pay for a marketplace search every time.
    public string? CandidatesJson { get; set; }
    public DateTime? CandidatesGeneratedAt { get; set; }

    /// <summary>
    /// Per-provider search outcome, so an empty candidate list can say whether a
    /// marketplace is unconfigured, erroring, or simply had nothing.
    /// </summary>
    public string? MarketplaceStatusJson { get; set; }
}
