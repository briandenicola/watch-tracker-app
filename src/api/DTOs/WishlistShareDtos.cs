using System.ComponentModel.DataAnnotations;
using WatchTracker.Api.Models;

namespace WatchTracker.Api.DTOs;

/// <summary>The owner's view of their wish list link.</summary>
public class WishlistShareDto
{
    public required string Token { get; set; }

    /// <summary>The full link, when an administrator has set a public address for shares.</summary>
    public string? Url { get; set; }

    /// <summary>Where the link lives, relative to the app's own origin.</summary>
    public required string Path { get; set; }

    public bool IncludePrices { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastViewedAt { get; set; }
    public int ViewCount { get; set; }
}

/// <summary>
/// One item on a shared wish list. The same allow-list as a shared watch, so a
/// column added to Watch cannot quietly become public, plus the target price —
/// which is present only when the owner asked for it.
/// </summary>
public class SharedWishlistItemDto
{
    public required string Brand { get; set; }
    public required string Model { get; set; }
    public string? Sku { get; set; }
    public MovementType MovementType { get; set; }
    public double? CaseSizeMm { get; set; }
    public string? CaseShape { get; set; }
    public string? DialColor { get; set; }
    public string? BandType { get; set; }
    public string? BandColor { get; set; }
    public string? WaterResistance { get; set; }
    public string? CountryOfOrigin { get; set; }
    public string? LinkUrl { get; set; }
    public string? LinkText { get; set; }

    /// <summary>What the owner hopes to pay, when they chose to publish prices.</summary>
    public decimal? TargetPrice { get; set; }

    public List<WatchImageDto> ImageUrls { get; set; } = [];
}

public class SharedWishlistDto
{
    /// <summary>The owner's display name, so a visitor knows whose list they are reading.</summary>
    public required string OwnerName { get; set; }

    public bool IncludesPrices { get; set; }
    public List<SharedWishlistItemDto> Items { get; set; } = [];
    public DateTime SharedAt { get; set; }
}

public class UpdateWishlistShareDto
{
    public bool IncludePrices { get; set; }
}
