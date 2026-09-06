using Microsoft.Extensions.Logging.Abstractions;
using WatchTracker.Api.Services;

namespace WatchTracker.Api.Tests;

public class CatalogMarketplaceSearchClientTests
{
    [Fact]
    public async Task Search_returns_usd_listings_from_approved_vendor_snippets()
    {
        var observedAt = DateTime.UtcNow;
        var search = new StubWebSearch(new WebSearchResult(
            WebSearchStatus.Success,
            [
                new WebSearchResultItem(
                    "Tudor Black Bay 36 - Pre-Owned",
                    "Available for $1,950 USD",
                    "https://shop.example.test/tudor-black-bay-36",
                    observedAt)
            ]));
        var client = CreateClient(
            search);

        var result = await client.SearchAsync("Tudor Black Bay 36 watch");

        Assert.Equal(MarketplaceSearchStatus.Success, result.Status);
        var listing = Assert.Single(result.Listings);
        Assert.Equal("Snippet shop", listing.Provider);
        Assert.Equal(1950m, listing.Price);
        Assert.Equal("USD", listing.Currency);
        Assert.Equal("Pre-owned", listing.Condition);
        Assert.Equal(observedAt, listing.ObservedAt);
        Assert.Null(listing.TotalPrice);
        Assert.Equal(16, listing.ProviderItemId.Length);
        Assert.Contains("site:shop.example.test", search.LastQuery);
    }

    [Fact]
    public async Task Search_rejects_results_outside_the_approved_vendor_domain()
    {
        var client = CreateClient(
            new StubWebSearch(new WebSearchResult(
                WebSearchStatus.Success,
                [
                    new WebSearchResultItem(
                        "Tudor Black Bay 36",
                        "$1,950",
                        "https://attacker.example/tudor",
                        DateTime.UtcNow)
                ])));

        var result = await client.SearchAsync("Tudor Black Bay 36 watch");

        Assert.Equal(MarketplaceSearchStatus.Success, result.Status);
        Assert.Empty(result.Listings);
    }

    [Fact]
    public async Task Search_reports_an_unconfigured_web_provider()
    {
        var client = CreateClient(
            new StubWebSearch(new WebSearchResult(
                WebSearchStatus.NotConfigured,
                [],
                "missing key")));

        var result = await client.SearchAsync("Tudor Black Bay 36 watch");

        Assert.Equal(MarketplaceSearchStatus.NotConfigured, result.Status);
        Assert.Empty(result.Listings);
        Assert.Equal("missing key", result.Error);
    }

    [Fact]
    public async Task Search_does_not_mistake_audemars_for_the_aud_currency()
    {
        var client = CreateClient(
            new StubWebSearch(new WebSearchResult(
                WebSearchStatus.Success,
                [
                    new WebSearchResultItem(
                        "Audemars Piguet Royal Oak",
                        "Available for $19,500",
                        "https://shop.example.test/royal-oak",
                        DateTime.UtcNow)
                ])));

        var result = await client.SearchAsync("Audemars Piguet Royal Oak");

        Assert.Single(result.Listings);
    }

    [Fact]
    public async Task Search_rejects_a_snippet_with_multiple_different_prices()
    {
        var client = CreateClient(
            new StubWebSearch(new WebSearchResult(
                WebSearchStatus.Success,
                [
                    new WebSearchResultItem(
                        "Tudor Black Bay 36",
                        "Was $2,400, now $1,950",
                        "https://shop.example.test/tudor",
                        DateTime.UtcNow)
                ])));

        var result = await client.SearchAsync("Tudor Black Bay 36");

        Assert.Empty(result.Listings);
    }

    private static CatalogMarketplaceSearchClient CreateClient(IWebSearchClient webSearch) =>
        new(
            [webSearch],
            new StubSettings(),
            new StubCatalog(),
            NullLogger<CatalogMarketplaceSearchClient>.Instance);

    private sealed class StubWebSearch(WebSearchResult result) : IWebSearchClient
    {
        public string ProviderName => "Brave";
        public string? LastQuery { get; private set; }

        public Task<WebSearchResult> SearchAsync(
            string query,
            CancellationToken ct = default)
        {
            LastQuery = query;
            return Task.FromResult(result);
        }
    }

    private sealed class StubCatalog : ISiteCatalog
    {
        public IReadOnlyList<SiteCatalogEntry> Sites { get; } =
        [
            new("Snippet shop", "shop.example.test")
        ];
    }

    private sealed class StubSettings : IAppSettingsService
    {
        public Task<string> GetAsync(string key, string defaultValue = "") =>
            Task.FromResult(key switch
            {
                AppSettingsService.Keys.WebSearchProvider => "Brave",
                AppSettingsService.Keys.MarketplaceVendor => "Snippet shop",
                _ => defaultValue
            });

        public Task<int> GetIntAsync(string key, int defaultValue) => Task.FromResult(defaultValue);
        public Task SetAsync(string key, string value) => Task.CompletedTask;
        public Task<Dictionary<string, string>> GetAllAsync() =>
            Task.FromResult<Dictionary<string, string>>([]);
    }
}
