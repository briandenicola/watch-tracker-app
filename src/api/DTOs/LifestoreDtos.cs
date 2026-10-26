using WatchTracker.Api.Models;

namespace WatchTracker.Api.DTOs;

/// <summary>One page of the life store feed: every record the caller owns, in id order.</summary>
public class LifestoreRecordsPageDto
{
    public string Schema { get; set; } = LifestoreRecordsPageDto.SchemaName;
    public List<LifestoreRecordDto> Records { get; set; } = [];
    /// <summary>The token for the following page; null on the last page.</summary>
    public string? Next { get; set; }

    public const string SchemaName = "watchtracker.lifestore.records/1";
}

public class LifestoreRecordDto
{
    public string Kind { get; set; } = "watch";
    public required string Id { get; set; }
    /// <summary>The latest change to the watch or anything embedded in it.</summary>
    public DateTime UpdatedAt { get; set; }
    public required LifestoreWatchDto Data { get; set; }
}

/// <summary>A watch or wish list item with its wear log, resale values, labels and disposition.</summary>
public class LifestoreWatchDto
{
    public int Id { get; set; }
    public required string Brand { get; set; }
    public required string Model { get; set; }
    public MovementType MovementType { get; set; }
    public string? Category { get; set; }
    public double? CaseSizeMm { get; set; }
    public double? CaseThicknessMm { get; set; }
    public string? CaseMaterial { get; set; }
    public string? BandType { get; set; }
    public string? BandColor { get; set; }
    public string? DialColor { get; set; }
    public string? CrystalType { get; set; }
    public string? CaseShape { get; set; }
    public string? CrownType { get; set; }
    public string? CalendarType { get; set; }
    public string? DateComplication { get; set; }
    public string? BezelType { get; set; }
    public string? WaterResistance { get; set; }
    public double? LugWidthMm { get; set; }
    public double? LugToLugMm { get; set; }
    public int? PowerReserveHours { get; set; }
    public string? CountryOfOrigin { get; set; }
    public string? Sku { get; set; }
    public string? SerialNumber { get; set; }
    public int? ProductionYear { get; set; }
    public string? BatteryType { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public decimal? PurchasePrice { get; set; }
    public AcquisitionType AcquisitionType { get; set; }
    public string? AcquiredFrom { get; set; }
    public string? AcquisitionSourceUrl { get; set; }
    public string? Notes { get; set; }
    public decimal? CurrentResaleValue { get; set; }
    public DateTime? LastBatteryChangedDate { get; set; }
    public DateTime? WarrantyExpiryDate { get; set; }
    public DateTime? LastServicedDate { get; set; }
    public string? StorageLocation { get; set; }
    public int? StorageSlot { get; set; }
    public bool IsWishList { get; set; }
    public int? WishlistPriority { get; set; }
    public string? LinkUrl { get; set; }
    public int TimesWorn { get; set; }
    public DateTime? LastWornDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<string> Labels { get; set; } = [];
    public List<LifestoreWearDto> Wears { get; set; } = [];
    public List<LifestoreResaleValueDto> ResaleValues { get; set; } = [];
    public LifestoreDispositionDto? Disposition { get; set; }
}

public class LifestoreWearDto
{
    public int Id { get; set; }
    public DateTime WornDate { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
}

public class LifestoreResaleValueDto
{
    public int Id { get; set; }
    public decimal Value { get; set; }
    public ResaleValueSource Source { get; set; }
    public DateTime RecordedAt { get; set; }
}

public class LifestoreDispositionDto
{
    public DispositionType Type { get; set; }
    public DateTime DispositionDate { get; set; }
    public string? Notes { get; set; }
    public string? SoldTo { get; set; }
    public decimal? SalePrice { get; set; }
    public int? ReceivedWatchId { get; set; }
    public string? TradeDetails { get; set; }
    public string? OtherLabel { get; set; }
    public string? ReturnReason { get; set; }
    public string? ReturnedTo { get; set; }
    public decimal? RefundAmount { get; set; }
}
