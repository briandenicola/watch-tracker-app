using WatchTracker.Api.Models;

namespace WatchTracker.Api.DTOs;

/// <summary>The owner's view of a share link: the token, and how it is being used.</summary>
public class WatchShareDto
{
    public required string Token { get; set; }

    /// <summary>
    /// Where the link lives, relative to the app's own origin. The client turns
    /// this into an absolute URL, so the link is always correct for whatever
    /// host the owner is actually using.
    /// </summary>
    public required string Path { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? LastViewedAt { get; set; }
    public int ViewCount { get; set; }
}

/// <summary>
/// What a visitor without an account is allowed to see. This is an allow-list on
/// purpose: fields are absent unless they are listed here, so adding a column to
/// Watch can never quietly publish it. Deliberately missing — what the watch cost,
/// where it came from, its serial number, notes, AI analysis, resale estimates,
/// where it is stored, how often it is worn, whether it has been sold, and any
/// trace of who owns it.
/// </summary>
public class SharedWatchDto
{
    public required string Brand { get; set; }
    public required string Model { get; set; }
    public string? Sku { get; set; }
    public MovementType MovementType { get; set; }
    public double? CaseSizeMm { get; set; }
    public string? CaseShape { get; set; }
    public string? CrystalType { get; set; }
    public string? BezelType { get; set; }
    public string? CrownType { get; set; }
    public string? CalendarType { get; set; }
    public string? DialColor { get; set; }
    public string? BandType { get; set; }
    public string? BandColor { get; set; }
    public double? LugWidthMm { get; set; }
    public string? WaterResistance { get; set; }
    public int? PowerReserveHours { get; set; }
    public string? BatteryType { get; set; }
    public int? ProductionYear { get; set; }
    public string? CountryOfOrigin { get; set; }
    public string? LinkUrl { get; set; }
    public string? LinkText { get; set; }
    public bool IsWishList { get; set; }
    public List<WatchImageDto> ImageUrls { get; set; } = [];
    public DateTime SharedAt { get; set; }
}
