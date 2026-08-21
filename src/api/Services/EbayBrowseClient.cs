using System.Net.Http.Headers;
using System.Globalization;
using System.Text.Json;

namespace WatchTracker.Api.Services;

public class EbayBrowseClient(
    HttpClient httpClient,
    IEbayTokenProvider tokenProvider,
    ILogger<EbayBrowseClient> logger) : IEbayBrowseClient
{
    public string ProviderName => "eBay";

    public async Task<MarketplaceSearchResult> SearchAsync(string query, CancellationToken ct = default)
    {
        var token = await tokenProvider.GetAccessTokenAsync(ct);
        if (token is null)
        {
            logger.LogInformation("eBay access token unavailable; skipping eBay leg.");
            return new MarketplaceSearchResult(
                MarketplaceSearchStatus.NotConfigured,
                [],
                "eBay marketplace search is not configured.");
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"https://api.ebay.com/buy/browse/v1/item_summary/search?q={Uri.EscapeDataString(query)}&limit=25");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("X-EBAY-C-MARKETPLACE-ID", "EBAY-US");

        try
        {
            using var response = await httpClient.SendAsync(request, ct);
            var body = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("eBay Browse API error {Status}: {Body}", response.StatusCode, body);
                return new MarketplaceSearchResult(
                    MarketplaceSearchStatus.ProviderError,
                    [],
                    $"eBay returned HTTP {(int)response.StatusCode}.");
            }

            using var doc = JsonDocument.Parse(body);
            if (!doc.RootElement.TryGetProperty("itemSummaries", out var items))
                return new MarketplaceSearchResult(MarketplaceSearchStatus.Success, []);

            var observedAt = DateTime.UtcNow;
            var listings = new List<MarketplaceListingItem>();
            foreach (var item in items.EnumerateArray())
            {
                var providerItemId = ReadString(item, "itemId");
                var title = ReadString(item, "title");
                var itemUrl = ReadString(item, "itemWebUrl");
                if (providerItemId is null || title is null || itemUrl is null) continue;
                if (!Uri.TryCreate(itemUrl, UriKind.Absolute, out var parsedUrl)
                    || parsedUrl.Scheme is not ("http" or "https"))
                    continue;
                if (!item.TryGetProperty("price", out var priceElement)
                    || !TryReadMoney(priceElement, out var price, out var currency))
                    continue;

                var shippingPrice = TryReadShipping(item, currency);
                decimal? totalPrice = shippingPrice is decimal shipping ? price + shipping : null;
                var imageUrl = item.TryGetProperty("image", out var image)
                    && image.ValueKind == JsonValueKind.Object
                    ? ReadString(image, "imageUrl")
                    : null;
                if (imageUrl is not null
                    && (!Uri.TryCreate(imageUrl, UriKind.Absolute, out var parsedImageUrl)
                        || parsedImageUrl.Scheme != Uri.UriSchemeHttps))
                    imageUrl = null;
                string? sellerName = null;
                decimal? sellerFeedback = null;
                if (item.TryGetProperty("seller", out var seller)
                    && seller.ValueKind == JsonValueKind.Object)
                {
                    sellerName = ReadString(seller, "username");
                    sellerFeedback = ReadDecimal(seller, "feedbackPercentage");
                }

                listings.Add(new MarketplaceListingItem(
                    ProviderName,
                    providerItemId,
                    title,
                    itemUrl,
                    imageUrl,
                    price,
                    shippingPrice,
                    totalPrice,
                    currency,
                    ReadListingType(item),
                    ReadString(item, "condition"),
                    sellerName,
                    sellerFeedback,
                    observedAt));
            }

            return new MarketplaceSearchResult(MarketplaceSearchStatus.Success, listings);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "eBay Browse API returned malformed JSON.");
            return new MarketplaceSearchResult(
                MarketplaceSearchStatus.ProviderError,
                [],
                "eBay returned an unreadable response.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "eBay Browse API call failed; skipping eBay leg.");
            return new MarketplaceSearchResult(
                MarketplaceSearchStatus.ProviderError,
                [],
                "eBay marketplace search failed.");
        }
    }

    private static MarketplaceListingType ReadListingType(JsonElement item)
    {
        if (!item.TryGetProperty("buyingOptions", out var options)
            || options.ValueKind != JsonValueKind.Array)
            return MarketplaceListingType.Unknown;

        var values = options.EnumerateArray()
            .Where(option => option.ValueKind == JsonValueKind.String)
            .Select(option => option.GetString())
            .ToList();
        if (values.Any(value => value == "AUCTION"))
            return MarketplaceListingType.Auction;
        if (values.Any(value => value == "FIXED_PRICE"))
            return MarketplaceListingType.FixedPrice;
        return MarketplaceListingType.Unknown;
    }

    private static decimal? TryReadShipping(JsonElement item, string currency)
    {
        if (!item.TryGetProperty("shippingOptions", out var options)
            || options.ValueKind != JsonValueKind.Array)
            return null;

        foreach (var option in options.EnumerateArray())
        {
            if (!option.TryGetProperty("shippingCost", out var cost)
                || !TryReadMoney(cost, out var amount, out var shippingCurrency)
                || !shippingCurrency.Equals(currency, StringComparison.OrdinalIgnoreCase))
                continue;
            return amount;
        }

        return null;
    }

    private static bool TryReadMoney(
        JsonElement element,
        out decimal amount,
        out string currency)
    {
        amount = 0;
        currency = "";
        var value = ReadString(element, "value");
        currency = ReadString(element, "currency") ?? "";
        return value is not null
            && currency.Length == 3
            && decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out amount)
            && amount >= 0;
    }

    private static decimal? ReadDecimal(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var value)) return null;
        return value.ValueKind switch
        {
            JsonValueKind.Number when value.TryGetDecimal(out var number) => number,
            JsonValueKind.String when decimal.TryParse(
                value.GetString(),
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var number) => number,
            _ => null
        };
    }

    private static string? ReadString(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var value)
            || value.ValueKind != JsonValueKind.String)
            return null;
        var text = value.GetString()?.Trim();
        return string.IsNullOrEmpty(text) ? null : text;
    }
}
