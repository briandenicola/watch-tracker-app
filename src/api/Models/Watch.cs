namespace WatchTracker.Api.Models;

public class Watch
{
    public int Id { get; set; }
    public required string Brand { get; set; }
    public required string Model { get; set; }
    public MovementType MovementType { get; set; }
    public double? CaseSizeMm { get; set; }
    public string? BandType { get; set; }
    public string? BandColor { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public decimal? PurchasePrice { get; set; }
    public AcquisitionType AcquisitionType { get; set; } = AcquisitionType.New;
    public string? AcquiredFrom { get; set; }
    public string? AcquisitionSourceUrl { get; set; }
    public string? Notes { get; set; }
    public string? AiAnalysis { get; set; }
    public DateTime? LastWornDate { get; set; }
    public int TimesWorn { get; set; }
    public decimal? CurrentResaleValue { get; set; }
    public DateTime? ResaleValueUpdatedAt { get; set; }
    public string? CrystalType { get; set; }
    public string? CaseShape { get; set; }
    public string? CrownType { get; set; }
    public string? CalendarType { get; set; }
    public string? CountryOfOrigin { get; set; }
    public string? WaterResistance { get; set; }
    public double? LugWidthMm { get; set; }
    public string? DialColor { get; set; }
    public string? BezelType { get; set; }
    public int? PowerReserveHours { get; set; }
    public string? Sku { get; set; }
    public string? SerialNumber { get; set; }
    public int? ProductionYear { get; set; }
    public string? BatteryType { get; set; }
    public DateTime? LastBatteryChangedDate { get; set; }
    public string? LinkUrl { get; set; }
    public string? LinkText { get; set; }
    public string? MarketplaceProvider { get; set; }
    public string? MarketplaceItemId { get; set; }
    public string? MarketplaceCurrency { get; set; }
    public DateTime? MarketplaceObservedAt { get; set; }
    public string? StorageLocation { get; set; }
    public bool IsWishList { get; set; }
    public int? WishlistPriority { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public ICollection<WatchImage> Images { get; set; } = [];
    public ICollection<WearLog> WearLogs { get; set; } = [];
    public ICollection<ResaleValueEntry> ResaleValueEntries { get; set; } = [];
    public WatchDisposition? Disposition { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Optimistic concurrency token
    public uint RowVersion { get; set; }
}
