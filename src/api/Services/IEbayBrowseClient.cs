namespace WatchTracker.Api.Services;

public record EbayListingItem(decimal Price, string Currency, string Title);

public interface IEbayBrowseClient
{
    Task<List<EbayListingItem>> SearchAsync(string query, CancellationToken ct = default);
}
