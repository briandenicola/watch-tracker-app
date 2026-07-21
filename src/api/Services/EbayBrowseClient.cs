using System.Net.Http.Headers;
using System.Text.Json;

namespace WatchTracker.Api.Services;

public class EbayBrowseClient(
    HttpClient httpClient,
    IEbayTokenProvider tokenProvider,
    ILogger<EbayBrowseClient> logger) : IEbayBrowseClient
{
    public async Task<List<EbayListingItem>> SearchAsync(string query, CancellationToken ct = default)
    {
        var token = await tokenProvider.GetAccessTokenAsync(ct);
        if (token is null)
        {
            logger.LogInformation("eBay access token unavailable; skipping eBay leg.");
            return [];
        }

        var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"https://api.ebay.com/buy/browse/v1/item_summary/search?q={Uri.EscapeDataString(query)}&limit=25");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("X-EBAY-C-MARKETPLACE-ID", "EBAY-US");

        try
        {
            var response = await httpClient.SendAsync(request, ct);
            var body = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("eBay Browse API error {Status}: {Body}", response.StatusCode, body);
                return [];
            }

            using var doc = JsonDocument.Parse(body);
            if (!doc.RootElement.TryGetProperty("itemSummaries", out var items))
                return [];

            var listings = new List<EbayListingItem>();
            foreach (var item in items.EnumerateArray())
            {
                if (!item.TryGetProperty("price", out var priceEl)) continue;
                if (!priceEl.TryGetProperty("value", out var valueEl)) continue;
                if (!decimal.TryParse(valueEl.GetString(), out var price)) continue;

                var currency = priceEl.TryGetProperty("currency", out var c) ? c.GetString() ?? "USD" : "USD";
                var title = item.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "";
                listings.Add(new EbayListingItem(price, currency, title));
            }

            return listings;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "eBay Browse API call failed; skipping eBay leg.");
            return [];
        }
    }
}
