using WatchTracker.Api.Models;

namespace WatchTracker.Api.Services;

public enum MarketplaceSearchStatus
{
    Success,
    NotConfigured,
    ProviderError
}

public enum MarketplaceListingType
{
    FixedPrice,
    Auction,
    Unknown
}

public record MarketplaceListingItem(
    string Provider,
    string ProviderItemId,
    string Title,
    string ItemUrl,
    string? ImageUrl,
    decimal Price,
    decimal? ShippingPrice,
    decimal? TotalPrice,
    string Currency,
    MarketplaceListingType ListingType,
    string? Condition,
    string? SellerName,
    decimal? SellerFeedbackPercent,
    DateTime ObservedAt,
    string? Brand = null,
    string? Model = null,
    string? ReferenceNumber = null,
    MovementType? MovementType = null,
    double? CaseSizeMm = null,
    string? DialColor = null,
    string? BandType = null);

public record MarketplaceSearchResult(
    MarketplaceSearchStatus Status,
    IReadOnlyList<MarketplaceListingItem> Listings,
    string? Error = null);

public interface IMarketplaceSearchClient
{
    string ProviderName { get; }
    Task<MarketplaceSearchResult> SearchAsync(string query, CancellationToken ct = default);
}
