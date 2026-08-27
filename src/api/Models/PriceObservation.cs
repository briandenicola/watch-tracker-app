namespace WatchTracker.Api.Models;

public class PriceObservation
{
    public int Id { get; set; }
    public int WatchId { get; set; }
    public Watch Watch { get; set; } = null!;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public required string Source { get; set; }
    public string? ProviderListingId { get; set; }
    public required string ListingKey { get; set; }
    public required string ListingUrl { get; set; }
    public required string ListingTitle { get; set; }
    public decimal Price { get; set; }
    public required string Currency { get; set; } = "USD";
    public string? Condition { get; set; }
    public PriceObservationKind Kind { get; set; }
    public PriceMatchConfidence MatchConfidence { get; set; }
    public DateTime ObservedAt { get; set; } = DateTime.UtcNow;
    public DateOnly ObservedOnUtc { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<PriceAlert> Alerts { get; set; } = [];
}
