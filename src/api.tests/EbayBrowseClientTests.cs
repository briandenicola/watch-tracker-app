using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using WatchTracker.Api.Services;

namespace WatchTracker.Api.Tests;

public class EbayBrowseClientTests
{
    [Fact]
    public async Task Search_normalizes_valid_listings_and_filters_malformed_items()
    {
        const string payload = """
            {
              "itemSummaries": [
                {
                  "itemId": "v1|123|0",
                  "title": "Example Watch",
                  "itemWebUrl": "https://www.ebay.com/itm/123",
                  "image": { "imageUrl": "https://i.ebayimg.com/123.jpg" },
                  "price": { "value": "1000.00", "currency": "USD" },
                  "buyingOptions": ["FIXED_PRICE"],
                  "shippingOptions": [
                    { "shippingCost": { "value": "25.00", "currency": "USD" } }
                  ],
                  "condition": "Pre-Owned",
                  "localizedAspects": [
                    { "name": "Brand", "value": "Hamilton" },
                    { "name": "Model", "value": "Khaki Field" },
                    { "name": "Movement", "value": "Mechanical (Automatic)" },
                    { "name": "Case Size", "value": "40 mm" },
                    { "name": "Dial Color", "value": "Black" }
                  ],
                  "seller": { "username": "seller", "feedbackPercentage": "99.8" }
                },
                {
                  "itemId": "missing-url",
                  "title": "Malformed",
                  "price": { "value": "10.00", "currency": "USD" }
                }
              ]
            }
            """;
        var client = CreateClient(HttpStatusCode.OK, payload);

        var result = await client.SearchAsync("example");

        Assert.Equal(MarketplaceSearchStatus.Success, result.Status);
        var listing = Assert.Single(result.Listings);
        Assert.Equal("eBay", listing.Provider);
        Assert.Equal(1000m, listing.Price);
        Assert.Equal(25m, listing.ShippingPrice);
        Assert.Equal(1025m, listing.TotalPrice);
        Assert.Equal(MarketplaceListingType.FixedPrice, listing.ListingType);
        Assert.Equal("https://www.ebay.com/itm/123", listing.ItemUrl);
        Assert.Equal(99.8m, listing.SellerFeedbackPercent);
        Assert.Equal("Hamilton", listing.Brand);
        Assert.Equal("Khaki Field", listing.Model);
        Assert.Equal(WatchTracker.Api.Models.MovementType.Automatic, listing.MovementType);
        Assert.Equal(40, listing.CaseSizeMm);
        Assert.Equal("Black", listing.DialColor);
    }

    [Fact]
    public async Task Search_surfaces_provider_errors()
    {
        var client = CreateClient(HttpStatusCode.TooManyRequests, "{}");

        var result = await client.SearchAsync("example");

        Assert.Equal(MarketplaceSearchStatus.ProviderError, result.Status);
        Assert.Empty(result.Listings);
        Assert.Contains("429", result.Error);
    }

    [Fact]
    public async Task Auction_with_best_offer_is_not_classified_as_fixed_price()
    {
        const string payload = """
            {
              "itemSummaries": [{
                "itemId": "auction",
                "title": "Auction Watch",
                "itemWebUrl": "https://www.ebay.com/itm/auction",
                "price": { "value": "500.00", "currency": "USD" },
                "buyingOptions": ["AUCTION", "BEST_OFFER"]
              }]
            }
            """;
        var client = CreateClient(HttpStatusCode.OK, payload);

        var listing = Assert.Single((await client.SearchAsync("example")).Listings);

        Assert.Equal(MarketplaceListingType.Auction, listing.ListingType);
    }

    private static EbayBrowseClient CreateClient(HttpStatusCode status, string body)
    {
        var handler = new StubHandler(new HttpResponseMessage(status)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        });
        return new EbayBrowseClient(
            new HttpClient(handler),
            new StubTokenProvider(),
            NullLogger<EbayBrowseClient>.Instance);
    }

    private sealed class StubTokenProvider : IEbayTokenProvider
    {
        public Task<string?> GetAccessTokenAsync(CancellationToken ct = default) =>
            Task.FromResult<string?>("token");
    }

    private sealed class StubHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(response);
    }
}
