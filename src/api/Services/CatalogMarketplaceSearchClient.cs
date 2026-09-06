using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace WatchTracker.Api.Services;

public class CatalogMarketplaceSearchClient(
    IEnumerable<IWebSearchClient> webSearchClients,
    IAppSettingsService appSettings,
    ISiteCatalog siteCatalog,
    ILogger<CatalogMarketplaceSearchClient> logger) : IMarketplaceSearchClient
{
    private const decimal MaxPrice = 10_000_000m;
    private static readonly Regex UsdPrice = new(
        @"(?<![\p{L}\p{N}])(?:US\s?\$|\$|USD\s*)(?<price>\d{1,3}(?:,\d{3})*(?:\.\d{1,2})?|\d+(?:\.\d{1,2})?)(?![\p{L}\p{N}])",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    private static readonly Regex NonUsdCode = new(
        @"(?<![\p{L}])(?:CAD|AUD|EUR|GBP|JPY|CHF|INR|SEK|NOK)(?![\p{L}])",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    public string ProviderName => "Vendor search";

    public async Task<MarketplaceSearchResult> SearchAsync(
        string query,
        CancellationToken ct = default)
    {
        var configuredProvider = await appSettings.GetAsync(
            AppSettingsService.Keys.WebSearchProvider,
            "Brave");
        var client = webSearchClients.FirstOrDefault(candidate =>
                candidate.ProviderName.Equals(configuredProvider, StringComparison.OrdinalIgnoreCase))
            ?? webSearchClients.FirstOrDefault();
        if (client is null)
        {
            return new MarketplaceSearchResult(
                MarketplaceSearchStatus.NotConfigured,
                [],
                "No web search provider is registered.");
        }

        var sites = siteCatalog.Sites.Where(site => !site.IsBlocked).ToList();
        if (sites.Count == 0)
        {
            return new MarketplaceSearchResult(
                MarketplaceSearchStatus.NotConfigured,
                [],
                "No vendor sites are enabled for marketplace search.");
        }

        WebSearchResult result;
        try
        {
            result = await client.SearchAsync($"{query} watch price", ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Vendor marketplace search through {WebSearchProvider} failed.",
                client.ProviderName);
            return new MarketplaceSearchResult(
                MarketplaceSearchStatus.ProviderError,
                [],
                $"{client.ProviderName} could not search the configured vendor sites.");
        }
        if (result.Status != WebSearchStatus.Success)
        {
            return new MarketplaceSearchResult(
                result.Status == WebSearchStatus.NotConfigured
                    ? MarketplaceSearchStatus.NotConfigured
                    : MarketplaceSearchStatus.ProviderError,
                [],
                result.Error);
        }

        var listings = new Dictionary<string, MarketplaceListingItem>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in result.Items)
        {
            var site = sites.FirstOrDefault(candidate => IsListingOnSite(item.Url, candidate.Domain));
            if (site is null
                || !TryReadUsdPrice($"{item.Title} {item.Description}", out var price)
                || !Uri.TryCreate(item.Url, UriKind.Absolute, out var uri)
                || uri.Scheme is not ("http" or "https"))
                continue;

            var normalizedUrl = uri.ToString();
            var listingId = StableListingId(site.Name, normalizedUrl);
            listings.TryAdd(
                $"{site.Name}:{listingId}",
                new MarketplaceListingItem(
                    site.Name,
                    listingId,
                    item.Title.Trim(),
                    normalizedUrl,
                    null,
                    price,
                    null,
                    null,
                    "USD",
                    MarketplaceListingType.FixedPrice,
                    InferCondition($"{item.Title} {item.Description}"),
                    null,
                    null,
                    item.ObservedAt.Kind == DateTimeKind.Utc
                        ? item.ObservedAt
                        : item.ObservedAt.ToUniversalTime()));
        }

        logger.LogInformation(
            "Vendor marketplace search completed through {WebSearchProvider}: {ListingCount} listings parsed from approved sites.",
            client.ProviderName,
            listings.Count);

        return new MarketplaceSearchResult(
            MarketplaceSearchStatus.Success,
            listings.Values.ToList());
    }

    private static bool TryReadUsdPrice(string text, out decimal price)
    {
        price = 0;
        if (text.Contains('€') || text.Contains('£') || text.Contains('¥')
            || NonUsdCode.IsMatch(text))
            return false;

        var prices = UsdPrice.Matches(text)
            .Select(match => decimal.TryParse(
                    match.Groups["price"].Value,
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out var parsed)
                ? parsed
                : 0)
            .Where(parsed => parsed is > 0 and <= MaxPrice)
            .Distinct()
            .ToList();
        if (prices.Count != 1) return false;
        price = prices[0];
        return true;
    }

    private static bool IsListingOnSite(string url, string domain) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri)
        && (uri.Host.Equals(domain, StringComparison.OrdinalIgnoreCase)
            || uri.Host.EndsWith($".{domain}", StringComparison.OrdinalIgnoreCase));

    private static string StableListingId(string source, string listingUrl)
    {
        var uri = new Uri(listingUrl);
        var canonicalUrl = uri.GetComponents(
            UriComponents.SchemeAndServer | UriComponents.Path,
            UriFormat.Unescaped);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{source}\n{canonicalUrl}"));
        return Convert.ToHexString(bytes)[..16];
    }

    private static string? InferCondition(string text)
    {
        if (text.Contains("pre-owned", StringComparison.OrdinalIgnoreCase)
            || text.Contains("preowned", StringComparison.OrdinalIgnoreCase))
            return "Pre-owned";
        if (text.Contains("used", StringComparison.OrdinalIgnoreCase))
            return "Used";
        if (text.Contains("new", StringComparison.OrdinalIgnoreCase)
            || text.Contains("unworn", StringComparison.OrdinalIgnoreCase))
            return "New";
        return null;
    }
}
