using WatchTracker.Api.Models;

namespace WatchTracker.Api.DTOs;

public class WatchDto
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
    public string? Notes { get; set; }
    public string? AiAnalysis { get; set; }
    public DateTime? LastWornDate { get; set; }
    public int TimesWorn { get; set; }
    public List<WatchImageDto> ImageUrls { get; set; } = [];
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
    public string? SerialNumber { get; set; }
    public string? BatteryType { get; set; }
    public string? LinkUrl { get; set; }
    public string? LinkText { get; set; }
    public bool IsWishList { get; set; }
    public string BrandName => Brand;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
