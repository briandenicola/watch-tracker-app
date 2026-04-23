using System.ComponentModel.DataAnnotations;
using WatchTracker.Api.Models;

namespace WatchTracker.Api.DTOs;

public class CreateWatchDto
{
    [Required, StringLength(200, MinimumLength = 1)]
    public required string Brand { get; set; }

    [Required, StringLength(200, MinimumLength = 1)]
    public required string Model { get; set; }

    public MovementType MovementType { get; set; }

    [Range(1, 200)]
    public double? CaseSizeMm { get; set; }

    [StringLength(100)]
    public string? BandType { get; set; }

    [StringLength(100)]
    public string? BandColor { get; set; }

    public DateTime? PurchaseDate { get; set; }

    [Range(0, 10_000_000)]
    public decimal? PurchasePrice { get; set; }

    [StringLength(10000)]
    public string? Notes { get; set; }

    [StringLength(100)]
    public string? CrystalType { get; set; }

    [StringLength(100)]
    public string? CaseShape { get; set; }

    [StringLength(100)]
    public string? CrownType { get; set; }

    [StringLength(100)]
    public string? CalendarType { get; set; }

    [StringLength(100)]
    public string? CountryOfOrigin { get; set; }

    [StringLength(100)]
    public string? WaterResistance { get; set; }

    [Range(1, 100)]
    public double? LugWidthMm { get; set; }

    [StringLength(100)]
    public string? DialColor { get; set; }

    [StringLength(100)]
    public string? BezelType { get; set; }

    [Range(0, 10000)]
    public int? PowerReserveHours { get; set; }

    [StringLength(200)]
    public string? SerialNumber { get; set; }

    [StringLength(100)]
    public string? BatteryType { get; set; }

    [StringLength(2000), Url]
    public string? LinkUrl { get; set; }

    [StringLength(200)]
    public string? LinkText { get; set; }

    public bool IsWishList { get; set; }
}