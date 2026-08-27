using System.ComponentModel.DataAnnotations;
using WatchTracker.Api.Models;
using WatchTracker.Api.Services;

namespace WatchTracker.Api.DTOs;

public class UpdatePriceMonitoringDto
{
    public bool PriceAlertEnabled { get; set; }

    [Range(0.01, 10_000_000)]
    public decimal? PriceAlertTarget { get; set; }
}

public class PriceMonitoringDto
{
    public bool PriceAlertEnabled { get; set; }
    public decimal? PriceAlertTarget { get; set; }
    public DateTime? PriceCheckedAt { get; set; }
}

public class PriceObservationDto
{
    public int Id { get; set; }
    public required string Source { get; set; }
    public string? ProviderListingId { get; set; }
    public required string ListingUrl { get; set; }
    public required string ListingTitle { get; set; }
    public decimal Price { get; set; }
    public required string Currency { get; set; }
    public string? Condition { get; set; }
    public PriceObservationKind Kind { get; set; }
    public PriceMatchConfidence MatchConfidence { get; set; }
    public DateTime ObservedAt { get; set; }
}

public class PriceScanSourceResultDto
{
    public required string Source { get; set; }
    public PriceScanStatus Status { get; set; }
    public string? Error { get; set; }
    public List<PriceObservationDto> Listings { get; set; } = [];
}

public class PriceScanResultDto
{
    public int WatchId { get; set; }
    public DateTime CheckedAt { get; set; }
    public List<PriceScanSourceResultDto> Sources { get; set; } = [];
    public int ObservationsAdded { get; set; }
    public int AlertsCreated { get; set; }
}

public class PriceAlertDto
{
    public int Id { get; set; }
    public int WatchId { get; set; }
    public required string WatchBrand { get; set; }
    public required string WatchModel { get; set; }
    public PriceAlertTrigger Trigger { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public required PriceObservationDto Observation { get; set; }
}
