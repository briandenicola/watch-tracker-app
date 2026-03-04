using WatchTracker.Api.Models;

namespace WatchTracker.Api.DTOs;

public class UpdateWatchDto
{
    public required string Brand { get; set; }
    public required string Model { get; set; }
    public MovementType MovementType { get; set; }
    public double? CaseSizeMm { get; set; }
    public string? BandType { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public decimal? PurchasePrice { get; set; }
    public string? Notes { get; set; }
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
    public string? LinkUrl { get; set; }
    public string? LinkText { get; set; }
}