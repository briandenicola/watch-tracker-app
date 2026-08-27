using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using WatchTracker.Api.Data;
using WatchTracker.Api.DTOs;
using WatchTracker.Api.Models;

namespace WatchTracker.Api.Services;

public class WishlistPriceScanner(
    AppDbContext context,
    IAppSettingsService appSettings,
    IEnumerable<IWebSearchClient> webSearchClients,
    IEbayBrowseClient ebayBrowseClient,
    ISiteCatalog siteCatalog,
    IPriceAlertEvaluator alertEvaluator,
    ILogger<WishlistPriceScanner> logger) : IWishlistPriceScanner
{
    private const decimal MaxPrice = 10_000_000m;
    private const int MinIntervalHours = 1;
    private const int MaxIntervalHours = 168;
    private static readonly Regex UsdPrice = new(
        @"(?<![\p{L}\p{N}])(?:US\s?\$|\$|USD\s*)(?<price>\d{1,3}(?:,\d{3})*(?:\.\d{1,2})?|\d+(?:\.\d{1,2})?)(?![\p{L}\p{N}])",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    private static readonly string[] NonUsdMarkers =
    [
        "€", "£", "¥", "cad", "aud", "eur", "gbp", "jpy", "chf", "inr", "sek", "nok"
    ];

    public async Task<PriceScanResultDto?> ScanAsync(
        int watchId,
        int userId,
        CancellationToken ct = default)
    {
        var watch = await context.Watches
            .FirstOrDefaultAsync(w => w.Id == watchId && w.UserId == userId, ct);
        if (watch is null) return null;
        if (!watch.IsWishList || watch.Disposition is not null)
            throw new InvalidOperationException("Price scanning is available only for active wish list watches.");

        return await ScanWatchAsync(watch, ct);
    }

    public async Task<int> ScanDueAsync(CancellationToken ct = default)
    {
        var configuredHours = await appSettings.GetIntAsync(
            AppSettingsService.Keys.PriceAlertScanIntervalHours,
            24);
        var intervalHours = Math.Clamp(configuredHours, MinIntervalHours, MaxIntervalHours);
        var cutoff = DateTime.UtcNow.AddHours(-intervalHours);
        var dueWatches = await context.Watches
            .Where(w => w.IsWishList && w.Disposition == null && w.PriceAlertEnabled)
            .Where(w => w.PriceCheckedAt == null || w.PriceCheckedAt < cutoff)
            .ToListAsync(ct);

        var scanned = 0;
        foreach (var watch in dueWatches)
        {
            try
            {
                await ScanWatchAsync(watch, ct);
                scanned++;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "Scheduled price scan failed for wish list watch {WatchId}.",
                    watch.Id);
            }
        }

        return scanned;
    }

    public async Task<IReadOnlyList<PriceObservationDto>?> GetObservationsAsync(
        int watchId,
        int userId,
        CancellationToken ct = default)
    {
        var isOwnedWishlist = await context.Watches
            .AnyAsync(w => w.Id == watchId && w.UserId == userId && w.IsWishList, ct);
        if (!isOwnedWishlist) return null;

        var observations = await context.PriceObservations
            .AsNoTracking()
            .Where(o => o.WatchId == watchId && o.UserId == userId)
            .OrderByDescending(o => o.ObservedAt)
            .Take(200)
            .ToListAsync(ct);
        return observations.Select(Map).ToList();
    }

    public async Task<PriceMonitoringDto?> UpdateMonitoringAsync(
        int watchId,
        int userId,
        UpdatePriceMonitoringDto dto,
        CancellationToken ct = default)
    {
        var watch = await context.Watches
            .FirstOrDefaultAsync(w => w.Id == watchId && w.UserId == userId, ct);
        if (watch is null) return null;
        if (!watch.IsWishList || watch.Disposition is not null)
            throw new InvalidOperationException("Price monitoring is available only for active wish list watches.");

        watch.PriceAlertEnabled = dto.PriceAlertEnabled;
        watch.PriceAlertTarget = dto.PriceAlertTarget;
        watch.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(ct);
        return new PriceMonitoringDto
        {
            PriceAlertEnabled = watch.PriceAlertEnabled,
            PriceAlertTarget = watch.PriceAlertTarget,
            PriceCheckedAt = watch.PriceCheckedAt
        };
    }

    private async Task<PriceScanResultDto> ScanWatchAsync(Watch watch, CancellationToken ct)
    {
        var checkedAt = DateTime.UtcNow;
        var result = new PriceScanResultDto
        {
            WatchId = watch.Id,
            CheckedAt = checkedAt
        };
        var configuredProvider = await appSettings.GetAsync(
            AppSettingsService.Keys.WebSearchProvider,
            "Brave");
        var webSearch = webSearchClients.FirstOrDefault(client =>
                client.ProviderName.Equals(configuredProvider, StringComparison.OrdinalIgnoreCase))
            ?? webSearchClients.FirstOrDefault();

        foreach (var site in siteCatalog.Sites)
        {
            var source = new PriceScanSourceResultDto { Source = site.Name };
            result.Sources.Add(source);
            if (site.IsBlocked)
            {
                source.Status = PriceScanStatus.Blocked;
                source.Error = site.BlockReason ?? "This source is not enabled for scanning.";
                continue;
            }

            if (webSearch is null)
            {
                source.Status = PriceScanStatus.NotConfigured;
                source.Error = "No web search provider is registered.";
                continue;
            }

            try
            {
                var search = await webSearch.SearchAsync(BuildSearchQuery(watch, site), ct);
                source.Status = Map(search.Status);
                source.Error = search.Error;
                if (search.Status != WebSearchStatus.Success) continue;

                foreach (var item in search.Items)
                {
                    if (!IsListingOnSite(item.Url, site.Domain)
                        || !TryCreateCandidate(
                            site.Name,
                            item.Url,
                            item.Title,
                            item.Description,
                            null,
                            item.ObservedAt,
                            out var candidate))
                        continue;

                    await AddCandidateAsync(watch, candidate, source, result, ct);
                }

                if (source.Listings.Count > 0)
                    source.Status = PriceScanStatus.Found;
                else
                    source.Status = PriceScanStatus.NoMatch;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Price scan search leg failed for {Site}.", site.Name);
                source.Status = PriceScanStatus.ProviderError;
                source.Error = "The configured web search provider could not complete this source.";
            }
        }

        await ScanEbayAsync(watch, result, ct);

        watch.PriceCheckedAt = checkedAt;
        watch.UpdatedAt = checkedAt;
        await context.SaveChangesAsync(ct);
        logger.LogInformation(
            "Price scan for wish list watch {WatchId}: {ObservationsAdded} observations, {AlertsCreated} alerts.",
            watch.Id,
            result.ObservationsAdded,
            result.AlertsCreated);
        return result;
    }

    private async Task ScanEbayAsync(
        Watch watch,
        PriceScanResultDto result,
        CancellationToken ct)
    {
        var source = new PriceScanSourceResultDto { Source = ebayBrowseClient.ProviderName };
        result.Sources.Add(source);
        try
        {
            var search = await ebayBrowseClient.SearchAsync(BuildSearchQuery(watch, null), ct);
            source.Status = search.Status switch
            {
                MarketplaceSearchStatus.NotConfigured => PriceScanStatus.NotConfigured,
                MarketplaceSearchStatus.ProviderError => PriceScanStatus.ProviderError,
                _ => PriceScanStatus.NoMatch
            };
            source.Error = search.Error;
            if (search.Status != MarketplaceSearchStatus.Success) return;

            foreach (var listing in search.Listings)
            {
                if (listing.ListingType == MarketplaceListingType.Auction
                    || !string.Equals(listing.Currency, "USD", StringComparison.OrdinalIgnoreCase)
                    || !TryCreateCandidate(
                        ebayBrowseClient.ProviderName,
                        listing.ItemUrl,
                        listing.Title,
                        listing.Condition ?? "",
                        listing.ProviderItemId,
                        listing.ObservedAt,
                        out var candidate,
                        listing.Price,
                        listing.Condition))
                    continue;

                await AddCandidateAsync(watch, candidate, source, result, ct);
            }

            if (source.Listings.Count > 0)
                source.Status = PriceScanStatus.Found;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Price scan eBay leg failed.");
            source.Status = PriceScanStatus.ProviderError;
            source.Error = "eBay could not complete this source.";
        }
    }

    private async Task AddCandidateAsync(
        Watch watch,
        PriceCandidate candidate,
        PriceScanSourceResultDto source,
        PriceScanResultDto result,
        CancellationToken ct)
    {
        var match = PriceObservationMatcher.Match(watch, candidate.Title, candidate.Price);
        var stored = await StoreAsync(watch, candidate, match.Confidence, ct);
        source.Listings.Add(Map(stored.Observation));
        if (!stored.Added) return;

        result.ObservationsAdded++;
        result.AlertsCreated += await alertEvaluator.EvaluateAsync(stored.Observation, watch, ct);
    }

    private async Task<StoredObservation> StoreAsync(
        Watch watch,
        PriceCandidate candidate,
        PriceMatchConfidence confidence,
        CancellationToken ct)
    {
        var listingKey = ListingKey(candidate.Source, candidate.ProviderListingId, candidate.ListingUrl);
        var observedOnUtc = DateOnly.FromDateTime(candidate.ObservedAt.ToUniversalTime());
        var existing = await context.PriceObservations
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.WatchId == watch.Id
                && o.Source == candidate.Source
                && o.ListingKey == listingKey
                && o.Price == candidate.Price
                && o.ObservedOnUtc == observedOnUtc, ct);
        if (existing is not null) return new StoredObservation(existing, false);

        var observation = new PriceObservation
        {
            WatchId = watch.Id,
            UserId = watch.UserId,
            Source = candidate.Source,
            ProviderListingId = candidate.ProviderListingId,
            ListingKey = listingKey,
            ListingUrl = candidate.ListingUrl,
            ListingTitle = candidate.Title,
            Price = candidate.Price,
            Currency = "USD",
            Condition = candidate.Condition,
            Kind = candidate.Kind,
            MatchConfidence = confidence,
            ObservedAt = candidate.ObservedAt.ToUniversalTime(),
            ObservedOnUtc = observedOnUtc,
            CreatedAt = DateTime.UtcNow
        };
        context.PriceObservations.Add(observation);

        try
        {
            await context.SaveChangesAsync(ct);
            return new StoredObservation(observation, true);
        }
        catch (DbUpdateException)
        {
            context.Entry(observation).State = EntityState.Detached;
            var racedObservation = await context.PriceObservations
                .AsNoTracking()
                .FirstAsync(o => o.WatchId == watch.Id
                    && o.Source == candidate.Source
                    && o.ListingKey == listingKey
                    && o.Price == candidate.Price
                    && o.ObservedOnUtc == observedOnUtc, ct);
            return new StoredObservation(racedObservation, false);
        }
    }

    private static bool TryCreateCandidate(
        string source,
        string url,
        string title,
        string detail,
        string? providerListingId,
        DateTime observedAt,
        out PriceCandidate candidate,
        decimal? knownPrice = null,
        string? knownCondition = null)
    {
        candidate = null!;
        if (!TryNormalizeUrl(url, out var normalizedUrl)
            || string.IsNullOrWhiteSpace(title)
            || title.Trim().Length > 500)
            return false;

        var text = $"{title} {detail}";
        decimal resolvedPrice;
        if (knownPrice is not null)
        {
            if (knownPrice is <= 0 or > MaxPrice) return false;
            resolvedPrice = knownPrice.Value;
        }
        else if (!TryReadUsdPrice(text, out resolvedPrice))
        {
            return false;
        }

        var condition = TrimTo(knownCondition ?? InferCondition(text), 200);
        candidate = new PriceCandidate(
            source,
            normalizedUrl,
            title.Trim(),
            TrimTo(providerListingId, 200),
            resolvedPrice,
            condition,
            InferKind(text, condition),
            observedAt.Kind == DateTimeKind.Utc ? observedAt : observedAt.ToUniversalTime());
        return true;
    }

    private static bool TryReadUsdPrice(string text, out decimal price)
    {
        price = 0;
        if (NonUsdMarkers.Any(marker =>
                text.Contains(marker, StringComparison.OrdinalIgnoreCase)))
            return false;

        var match = UsdPrice.Match(text);
        return match.Success
            && decimal.TryParse(
                match.Groups["price"].Value,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out price)
            && price is > 0 and <= MaxPrice;
    }

    private static PriceObservationKind InferKind(string text, string? condition)
    {
        var evidence = $"{text} {condition}";
        if (evidence.Contains("pre-owned", StringComparison.OrdinalIgnoreCase)
            || evidence.Contains("preowned", StringComparison.OrdinalIgnoreCase)
            || evidence.Contains("used", StringComparison.OrdinalIgnoreCase)
            || evidence.Contains("vintage", StringComparison.OrdinalIgnoreCase))
            return PriceObservationKind.Preowned;
        if (evidence.Contains("new", StringComparison.OrdinalIgnoreCase)
            || evidence.Contains("unworn", StringComparison.OrdinalIgnoreCase))
            return PriceObservationKind.New;
        return PriceObservationKind.Unknown;
    }

    private static string? InferCondition(string text)
    {
        if (text.Contains("pre-owned", StringComparison.OrdinalIgnoreCase)
            || text.Contains("preowned", StringComparison.OrdinalIgnoreCase))
            return "Pre-owned";
        if (text.Contains("used", StringComparison.OrdinalIgnoreCase))
            return "Used";
        if (text.Contains("new", StringComparison.OrdinalIgnoreCase))
            return "New";
        return null;
    }

    private static string BuildSearchQuery(Watch watch, SiteCatalogEntry? site)
    {
        var reference = string.IsNullOrWhiteSpace(watch.Sku) ? "" : $" {watch.Sku}";
        var domain = site is null ? "" : $" site:{site.Domain}";
        return $"{watch.Brand} {watch.Model}{reference} watch price{domain}";
    }

    private static PriceScanStatus Map(WebSearchStatus status) => status switch
    {
        WebSearchStatus.NotConfigured => PriceScanStatus.NotConfigured,
        WebSearchStatus.ProviderError => PriceScanStatus.ProviderError,
        _ => PriceScanStatus.NoMatch
    };

    private static bool IsListingOnSite(string url, string domain) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri)
        && (uri.Host.Equals(domain, StringComparison.OrdinalIgnoreCase)
            || uri.Host.EndsWith($".{domain}", StringComparison.OrdinalIgnoreCase));

    private static bool TryNormalizeUrl(string url, out string normalized)
    {
        normalized = "";
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
            || uri.Scheme is not ("http" or "https")
            || url.Length > 2000)
            return false;
        normalized = uri.ToString();
        return true;
    }

    private static string ListingKey(string source, string? providerListingId, string listingUrl)
    {
        var identifier = string.IsNullOrWhiteSpace(providerListingId)
            ? new Uri(listingUrl).GetComponents(
                UriComponents.SchemeAndServer | UriComponents.Path,
                UriFormat.Unescaped)
            : providerListingId;
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{source}\n{identifier}"));
        return Convert.ToHexString(bytes);
    }

    private static string? TrimTo(string? value, int maxLength)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed[..Math.Min(trimmed.Length, maxLength)];
    }

    internal static PriceObservationDto Map(PriceObservation observation) => new()
    {
        Id = observation.Id,
        Source = observation.Source,
        ProviderListingId = observation.ProviderListingId,
        ListingUrl = observation.ListingUrl,
        ListingTitle = observation.ListingTitle,
        Price = observation.Price,
        Currency = observation.Currency,
        Condition = observation.Condition,
        Kind = observation.Kind,
        MatchConfidence = observation.MatchConfidence,
        ObservedAt = observation.ObservedAt
    };

    private sealed record PriceCandidate(
        string Source,
        string ListingUrl,
        string Title,
        string? ProviderListingId,
        decimal Price,
        string? Condition,
        PriceObservationKind Kind,
        DateTime ObservedAt);

    private sealed record StoredObservation(PriceObservation Observation, bool Added);
}
