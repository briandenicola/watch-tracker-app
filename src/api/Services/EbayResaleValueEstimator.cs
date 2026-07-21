using WatchTracker.Api.Models;

namespace WatchTracker.Api.Services;

public class EbayResaleValueEstimator(
    IEbayBrowseClient ebayBrowseClient,
    ILogger<EbayResaleValueEstimator> logger) : IResaleValueEstimator
{
    private const string SourceName = "eBay Listings";

    public async Task<ResaleEstimateResult?> EstimateAsync(Watch watch, CancellationToken ct = default)
    {
        var query = $"{watch.Brand} {watch.Model}";
        var listings = await ebayBrowseClient.SearchAsync(query, ct);
        if (listings.Count == 0)
        {
            logger.LogInformation("No eBay listings found for {Brand} {Model}; skipping eBay estimate.", watch.Brand, watch.Model);
            return null;
        }

        var prices = listings.Select(l => l.Price).OrderBy(p => p).ToList();
        var average = prices.Average();
        var min = prices.First();
        var max = prices.Last();

        var reasoning = $"Based on {prices.Count} active eBay listing(s) for \"{query}\", " +
            $"prices ranging {min:C}–{max:C} (average {average:C}).";

        return new ResaleEstimateResult(average, reasoning, SourceName);
    }
}
