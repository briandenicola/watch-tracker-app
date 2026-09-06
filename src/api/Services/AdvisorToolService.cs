using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WatchTracker.Api.Data;
using WatchTracker.Api.DTOs;
using WatchTracker.Api.Models;

namespace WatchTracker.Api.Services;

public class AdvisorToolService(
    AppDbContext db,
    ICollectionProfileService collectionProfile,
    IEnumerable<IMarketplaceSearchClient> marketplaceClients,
    IEnumerable<IWebSearchClient> webSearchClients,
    IAppSettingsService appSettings) : IAdvisorToolService
{
    public async Task<IReadOnlyList<AdvisorFeedbackMemoryDto>> GetRecentFeedbackAsync(
        int userId,
        CancellationToken ct = default) =>
        await db.AdvisorRecommendationFeedback
            .AsNoTracking()
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.UpdatedAt)
            .Take(10)
            .Select(f => new AdvisorFeedbackMemoryDto
            {
                Provider = f.Provider,
                Title = f.Title,
                Kind = f.Kind,
                Notes = f.Notes,
                UpdatedAt = f.UpdatedAt
            })
            .ToListAsync(ct);

    private const int MaxCollectionItems = 100;
    private const int MaxMarketplaceItems = 10;
    private const int MaxWebResults = 5;
    private const int MaxToolOutputLength = 12_000;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public string Instructions => """
        Approved tools and exact argument shapes:
        - collection_profile {}
          Returns deterministic coverage, gaps, redundancy, data quality, wear, resale freshness, and wishlist overlap.
        - collection_watches {}
          Returns the user's active watches and recorded attributes.
        - wishlist_context {}
          Returns the user's wishlist items and priorities.
        - marketplace_search {"query":"brand model watch","maxPrice":2000,"currency":"USD"}
          Returns current fixed-price listings. maxPrice is optional; currency is required with it.
        - resale_comparables {"brand":"Brand","model":"Model","currency":"USD"}
          Returns deterministic min/median/max active asking prices, not completed-sale prices.
        - web_research {"query":"brand or model question"}
          Returns current Brave or SearXNG snippets and source URLs.
        - score_listing {"provider":"eBay","providerItemId":"id","budget":2000,"currency":"USD"}
          Scores an observed fixed-price listing. budget and matching currency are optional.
        """;

    public async Task<AdvisorToolResult> ExecuteAsync(
        string toolName,
        JsonElement arguments,
        AdvisorToolContext context,
        CancellationToken ct = default)
    {
        if (arguments.ValueKind != JsonValueKind.Object)
            throw new InvalidOperationException($"Tool \"{toolName}\" requires a JSON object for arguments.");

        var execution = toolName switch
        {
            "collection_profile" => Completed(context.Profile),
            "collection_watches" => Completed(await GetCollectionWatchesAsync(context.UserId, ct)),
            "wishlist_context" => Completed(await GetWishlistAsync(context.UserId, ct)),
            "marketplace_search" => await SearchMarketplaceAsync(arguments, context, ct),
            "resale_comparables" => await GetResaleComparablesAsync(arguments, context, ct),
            "web_research" => await SearchWebAsync(arguments, context, ct),
            "score_listing" => Completed(await ScoreListingAsync(arguments, context, ct)),
            _ => throw new InvalidOperationException($"The advisor requested unknown tool \"{toolName}\".")
        };

        var json = JsonSerializer.Serialize(execution.Data, JsonOptions);
        if (json.Length > MaxToolOutputLength)
            throw new InvalidOperationException($"Tool \"{toolName}\" returned more data than the advisor can safely process.");

        return new AdvisorToolResult(
            json,
            new AdvisorToolActivityDto
            {
                Tool = toolName,
                Status = execution.Status,
                Message = execution.Message
            });
    }

    private static ToolExecution Completed(object data) => new(data, "completed", null);

    private async Task<object> GetCollectionWatchesAsync(int userId, CancellationToken ct)
    {
        var query = db.Watches
            .AsNoTracking()
            .Where(w => w.UserId == userId && !w.IsWishList && w.Disposition == null)
            .OrderBy(w => w.Id);
        var total = await query.CountAsync(ct);
        var watches = await query
            .Take(MaxCollectionItems)
            .Select(w => new
            {
                w.Id,
                w.Brand,
                w.Model,
                movementType = w.MovementType.ToString(),
                w.CaseSizeMm,
                w.DialColor,
                w.BandType,
                w.BandColor,
                w.CaseShape,
                w.BezelType,
                w.CalendarType,
                w.WaterResistance,
                w.PurchasePrice,
                w.CurrentResaleValue,
                w.ResaleValueUpdatedAt,
                w.TimesWorn,
                w.LastWornDate
            })
            .ToListAsync(ct);
        return new { total, truncated = total > watches.Count, watches };
    }

    private async Task<object> GetWishlistAsync(int userId, CancellationToken ct)
    {
        var query = db.Watches
            .AsNoTracking()
            .Where(w => w.UserId == userId && w.IsWishList)
            .OrderBy(w => w.WishlistPriority)
            .ThenBy(w => w.Id);
        var total = await query.CountAsync(ct);
        var watches = await query
            .Take(MaxCollectionItems)
            .Select(w => new
            {
                w.Id,
                w.Brand,
                w.Model,
                movementType = w.MovementType.ToString(),
                w.CaseSizeMm,
                w.DialColor,
                w.BandType,
                w.PurchasePrice,
                w.CurrentResaleValue,
                w.WishlistPriority,
                w.LinkUrl
            })
            .ToListAsync(ct);
        return new { total, truncated = total > watches.Count, watches };
    }

    private async Task<ToolExecution> SearchMarketplaceAsync(
        JsonElement arguments,
        AdvisorToolContext context,
        CancellationToken ct)
    {
        var query = RequiredString(arguments, "query", 200);
        var maxPrice = OptionalPositiveDecimal(arguments, "maxPrice");
        var currency = OptionalCurrency(arguments, "currency");
        if (maxPrice is not null && currency is null)
            throw new InvalidOperationException(
                "Tool argument \"currency\" is required when maxPrice is provided.");
        var providerResults = new List<object>();
        var listings = new List<MarketplaceListingItem>();

        foreach (var client in marketplaceClients)
        {
            var result = await client.SearchAsync(query, ct);
            providerResults.Add(new
            {
                provider = client.ProviderName,
                status = result.Status.ToString(),
                result.Error
            });
            if (result.Status != MarketplaceSearchStatus.Success) continue;
            listings.AddRange(result.Listings);
        }

        var selected = listings
            .Where(l => l.ListingType == MarketplaceListingType.FixedPrice)
            .Where(l => currency is null
                || l.Currency.Equals(currency, StringComparison.OrdinalIgnoreCase))
            .Where(l => maxPrice is null || l.TotalPrice is decimal total && total <= maxPrice)
            .OrderBy(l => l.TotalPrice ?? l.Price)
            .Take(MaxMarketplaceItems)
            .ToList();
        foreach (var listing in selected)
        {
            var key = AdvisorToolContext.ListingKey(listing.Provider, listing.ProviderItemId);
            context.Listings[key] = listing;
            context.Sources[listing.ItemUrl] = new AdvisorCitationDto
            {
                Title = listing.Title,
                Url = listing.ItemUrl,
                Provider = listing.Provider,
                ObservedAt = listing.ObservedAt
            };
        }

        var data = new
        {
            query,
            priceType = "active fixed asking price",
            maxPrice,
            currency,
            providers = providerResults,
            listings = selected
        };
        return ProviderExecution(data, providerResults, selected.Count > 0);
    }

    private async Task<ToolExecution> GetResaleComparablesAsync(
        JsonElement arguments,
        AdvisorToolContext context,
        CancellationToken ct)
    {
        var brand = RequiredString(arguments, "brand", 100);
        var model = RequiredString(arguments, "model", 150);
        var currency = RequiredCurrency(arguments, "currency");
        var providerResults = new List<object>();
        var observed = new List<MarketplaceListingItem>();
        foreach (var client in marketplaceClients)
        {
            var result = await client.SearchAsync($"{brand} {model} watch", ct);
            providerResults.Add(new
            {
                provider = client.ProviderName,
                status = result.Status.ToString(),
                result.Error
            });
            if (result.Status == MarketplaceSearchStatus.Success)
                observed.AddRange(result.Listings);
        }

        var selected = observed
            .Where(l => l.ListingType == MarketplaceListingType.FixedPrice)
            .Where(l => l.Currency.Equals(currency, StringComparison.OrdinalIgnoreCase))
            .Where(l => l.Title.Contains(brand, StringComparison.OrdinalIgnoreCase)
                && l.Title.Contains(model, StringComparison.OrdinalIgnoreCase))
            .Take(MaxMarketplaceItems)
            .ToList();
        foreach (var listing in selected)
        {
            var key = AdvisorToolContext.ListingKey(listing.Provider, listing.ProviderItemId);
            context.Listings[key] = listing;
            context.Sources[listing.ItemUrl] = new AdvisorCitationDto
            {
                Title = listing.Title,
                Url = listing.ItemUrl,
                Provider = listing.Provider,
                ObservedAt = listing.ObservedAt
            };
        }
        var matching = selected
            .Select(l => l.TotalPrice ?? l.Price)
            .OrderBy(price => price)
            .ToList();
        if (matching.Count == 0)
        {
            var noResults = new
            {
                brand,
                model,
                priceType = "active fixed asking price",
                count = 0,
                currency,
                providers = providerResults,
                message = "No matching active asking-price comparables were found."
            };
            return ProviderExecution(noResults, providerResults, false);
        }

        var middle = matching.Count / 2;
        var median = matching.Count % 2 == 0
            ? (matching[middle - 1] + matching[middle]) / 2m
            : matching[middle];
        var data = new
        {
            brand,
            model,
            priceType = "active fixed asking price",
            count = matching.Count,
            minimum = matching[0],
            median,
            maximum = matching[^1],
            currency,
            providers = providerResults
        };
        return ProviderExecution(data, providerResults, true);
    }

    private async Task<ToolExecution> SearchWebAsync(
        JsonElement arguments,
        AdvisorToolContext context,
        CancellationToken ct)
    {
        var query = RequiredString(arguments, "query", 250);
        var configuredProvider = await appSettings.GetAsync(
            AppSettingsService.Keys.WebSearchProvider,
            "Brave");
        var client = webSearchClients.FirstOrDefault(c =>
                c.ProviderName.Equals(configuredProvider, StringComparison.OrdinalIgnoreCase))
            ?? webSearchClients.FirstOrDefault();
        if (client is null)
            throw new InvalidOperationException("No web research provider is registered.");

        var result = await client.SearchAsync(query, ct);
        var items = result.Items.Take(MaxWebResults).ToList();
        foreach (var item in items)
        {
            context.Sources[item.Url] = new AdvisorCitationDto
            {
                Title = item.Title,
                Url = item.Url,
                Provider = client.ProviderName,
                ObservedAt = item.ObservedAt
            };
        }

        var data = new
        {
            query,
            provider = client.ProviderName,
            status = result.Status.ToString(),
            result.Error,
            results = items
        };
        var status = result.Status switch
        {
            WebSearchStatus.Success => "completed",
            WebSearchStatus.NotConfigured => "unavailable",
            _ => "failed"
        };
        return new ToolExecution(data, status, result.Error);
    }

    private async Task<object> ScoreListingAsync(
        JsonElement arguments,
        AdvisorToolContext context,
        CancellationToken ct)
    {
        var provider = RequiredString(arguments, "provider", 50);
        var providerItemId = RequiredString(arguments, "providerItemId", 200);
        var key = AdvisorToolContext.ListingKey(provider, providerItemId);
        if (!context.Listings.TryGetValue(key, out var listing))
            throw new InvalidOperationException(
                "score_listing can only score a listing returned by marketplace_search in this advisor turn.");

        var candidate = new CollectionCandidateProfile
        {
            Brand = listing.Brand,
            Model = listing.Model,
            MovementType = listing.MovementType,
            CaseSizeMm = listing.CaseSizeMm,
            DialColor = listing.DialColor,
            BandType = listing.BandType,
            Price = listing.TotalPrice
        };
        var budget = OptionalPositiveDecimal(arguments, "budget");
        var currency = OptionalCurrency(arguments, "currency");
        if (budget is not null && currency is null)
            throw new InvalidOperationException(
                "Tool argument \"currency\" is required when budget is provided.");
        if (budget is not null && listing.TotalPrice is null)
            throw new InvalidOperationException(
                "The listing cannot be budget-scored because its delivered total is unavailable.");
        if (currency is not null
            && !listing.Currency.Equals(currency, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                "The listing currency does not match the requested budget currency.");
        var score = collectionProfile.ScoreCandidate(context.Profile, candidate, budget);
        var overlappingWatchIds = await db.Watches
            .AsNoTracking()
            .Where(w => w.UserId == context.UserId && !w.IsWishList && w.Disposition == null)
            .Where(w => listing.Title.ToLower().Contains(w.Brand.ToLower())
                && listing.Title.ToLower().Contains(w.Model.ToLower()))
            .Select(w => w.Id)
            .ToListAsync(ct);
        if (overlappingWatchIds.Count > 0)
        {
            score.CollectionFitScore = Math.Max(0, score.CollectionFitScore - 30);
            score.TotalScore = score.BudgetFitScore is int budgetScore
                ? (int)Math.Round(
                    score.CollectionFitScore * 0.6
                    + budgetScore * 0.3
                    + score.EvidenceConfidencePercent * 0.1)
                : (int)Math.Round(
                    score.CollectionFitScore * 0.85
                    + score.EvidenceConfidencePercent * 0.15);
            score.Reasons.Add(
                "The observed listing title matches a brand and model already in the active collection.");
        }
        context.ListingScores[key] = score;

        return new
        {
            listing.Provider,
            listing.ProviderItemId,
            price = listing.TotalPrice ?? listing.Price,
            listing.Currency,
            overlappingWatchIds,
            score
        };
    }

    private static string RequiredString(JsonElement arguments, string property, int maxLength)
    {
        var value = OptionalString(arguments, property, maxLength);
        return value ?? throw new InvalidOperationException($"Tool argument \"{property}\" is required.");
    }

    private static string? OptionalString(JsonElement arguments, string property, int maxLength)
    {
        if (!arguments.TryGetProperty(property, out var element)
            || element.ValueKind == JsonValueKind.Null)
            return null;
        if (element.ValueKind != JsonValueKind.String)
            throw new InvalidOperationException($"Tool argument \"{property}\" must be a string.");
        var value = element.GetString()?.Trim();
        if (string.IsNullOrEmpty(value)) return null;
        if (value.Length > maxLength)
            throw new InvalidOperationException($"Tool argument \"{property}\" exceeds {maxLength} characters.");
        return value;
    }

    private static decimal? OptionalPositiveDecimal(JsonElement arguments, string property)
    {
        if (!arguments.TryGetProperty(property, out var element)
            || element.ValueKind == JsonValueKind.Null)
            return null;
        if (element.ValueKind != JsonValueKind.Number || !element.TryGetDecimal(out var value) || value <= 0)
            throw new InvalidOperationException($"Tool argument \"{property}\" must be a positive number.");
        return value;
    }

    private static string RequiredCurrency(JsonElement arguments, string property) =>
        OptionalCurrency(arguments, property)
        ?? throw new InvalidOperationException($"Tool argument \"{property}\" is required.");

    private static string? OptionalCurrency(JsonElement arguments, string property)
    {
        var value = OptionalString(arguments, property, 3)?.ToUpperInvariant();
        if (value is not null && (value.Length != 3 || value.Any(c => c is < 'A' or > 'Z')))
            throw new InvalidOperationException(
                $"Tool argument \"{property}\" must be a three-letter currency code.");
        return value;
    }

    private static ToolExecution ProviderExecution(
        object data,
        IEnumerable<object> providerResults,
        bool hasEvidence)
    {
        var elements = providerResults
            .Select(result => JsonSerializer.SerializeToElement(result, JsonOptions))
            .ToList();
        if (elements.Count == 0)
            return new ToolExecution(data, "unavailable", "No marketplace provider is registered.");
        var hasConfiguredProvider = elements.Any(element =>
            element.GetProperty("status").GetString() == "Success");
        var warnings = elements
            .Where(element =>
            {
                var status = element.GetProperty("status").GetString();
                return status == "ProviderError"
                    || (!hasConfiguredProvider && status != "Success");
            })
            .Select(element =>
                $"{element.GetProperty("provider").GetString()}: " +
                $"{element.GetProperty("error").GetString() ?? element.GetProperty("status").GetString()}")
            .ToList();
        var status = warnings.Count == 0
            ? "completed"
            : hasEvidence
                ? "completed_with_warnings"
                : "unavailable";
        return new ToolExecution(
            data,
            status,
            warnings.Count == 0 ? null : string.Join("; ", warnings));
    }

    private sealed record ToolExecution(object Data, string Status, string? Message);
}
