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
    DateTime ObservedAt);

public record MarketplaceSearchResult(
    MarketplaceSearchStatus Status,
    IReadOnlyList<MarketplaceListingItem> Listings,
    string? Error = null);

public interface IMarketplaceSearchClient
{
    string ProviderName { get; }
    Task<MarketplaceSearchResult> SearchAsync(string query, CancellationToken ct = default);
}
