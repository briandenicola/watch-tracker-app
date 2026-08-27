using System.Text.RegularExpressions;
using WatchTracker.Api.Models;

namespace WatchTracker.Api.Services;

public record PriceMatch(PriceMatchConfidence Confidence, string Reason);

/// <summary>
/// Deterministic identity guard for price sightings. It never relies on an LLM:
/// an ambiguous title can be recorded as a low-confidence lead, but never alert.
/// </summary>
public static class PriceObservationMatcher
{
    private static readonly HashSet<string> GenericModelTokens =
    [
        "watch", "watches", "automatic", "manual", "quartz", "chronograph",
        "diver", "dress", "classic", "edition", "mens", "men", "womens",
        "women", "new", "used", "preowned", "pre", "owned", "sale"
    ];

    private static readonly Regex TokenRegex = new(
        @"[\p{L}\p{N}]+",
        RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    public static PriceMatch Match(Watch watch, string listingTitle, decimal price)
    {
        var title = Normalize(listingTitle);
        var sku = Normalize(watch.Sku);
        if (sku.Length >= 3 && title.Contains(sku, StringComparison.Ordinal))
            return new PriceMatch(PriceMatchConfidence.High, "Exact SKU/reference confirmed.");

        var titleTokens = Tokens(listingTitle).ToHashSet(StringComparer.Ordinal);
        var brandTokens = Tokens(watch.Brand)
            .Where(token => token.Length >= 2)
            .ToList();
        var modelTokens = Tokens(watch.Model)
            .Where(IsMeaningfulModelToken)
            .ToList();

        var brandConfirmed = brandTokens.Count > 0 && brandTokens.All(titleTokens.Contains);
        if (!brandConfirmed)
            return new PriceMatch(PriceMatchConfidence.Low, "Listing title does not confirm the brand.");

        if (modelTokens.Count == 0)
            return new PriceMatch(
                PriceMatchConfidence.Low,
                "The saved model is too generic to confirm safely.");

        var modelConfirmed = modelTokens.All(titleTokens.Contains);
        if (!modelConfirmed)
        {
            var partial = modelTokens.Any(titleTokens.Contains);
            return new PriceMatch(
                partial ? PriceMatchConfidence.Medium : PriceMatchConfidence.Low,
                partial
                    ? "Listing title only partially confirms the model."
                    : "Listing title does not confirm the model.");
        }

        if (watch.PurchasePrice is decimal purchasePrice
            && !IsPlausible(price, purchasePrice))
        {
            return new PriceMatch(
                PriceMatchConfidence.Medium,
                "The listing price is outside the watch's plausibility band.");
        }

        return new PriceMatch(
            PriceMatchConfidence.High,
            "Brand and specific model confirmed by the listing title.");
    }

    private static bool IsMeaningfulModelToken(string token) =>
        !GenericModelTokens.Contains(token)
        && (token.Length >= 3 || token.Any(char.IsDigit));

    private static bool IsPlausible(decimal price, decimal purchasePrice)
    {
        if (purchasePrice <= 0) return true;
        return price >= purchasePrice / 10m && price <= purchasePrice * 10m;
    }

    private static string Normalize(string? value) =>
        string.Concat(Tokens(value));

    private static IEnumerable<string> Tokens(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? []
            : TokenRegex.Matches(value.ToLowerInvariant())
                .Select(match => match.Value);
}
