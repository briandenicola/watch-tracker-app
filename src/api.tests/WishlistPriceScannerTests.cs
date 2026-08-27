using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using WatchTracker.Api.Data;
using WatchTracker.Api.DTOs;
using WatchTracker.Api.Models;
using WatchTracker.Api.Services;

namespace WatchTracker.Api.Tests;

public class WishlistPriceScannerTests
{
    [Fact]
    public async Task Scan_reports_unconfigured_and_blocked_sources_without_calling_them_no_match()
    {
        await using var database = await TestDatabase.CreateAsync();
        var watch = await SeedWatchAsync(database, priceAlertsEnabled: false);
        var scanner = Build(
            database,
            new StubSearch { Status = WebSearchStatus.NotConfigured },
            new StubEbay { Status = MarketplaceSearchStatus.NotConfigured },
            new TestCatalog(
                new SiteCatalogEntry("Snippet shop", "shop.example.test"),
                new SiteCatalogEntry("Blocked shop", "blocked.example.test", true, "This source is blocked.")));

        var result = await scanner.ScanAsync(watch.Id, watch.UserId);

        Assert.NotNull(result);
        Assert.Collection(
            result.Sources,
            source =>
            {
                Assert.Equal("Snippet shop", source.Source);
                Assert.Equal(PriceScanStatus.NotConfigured, source.Status);
            },
            source =>
            {
                Assert.Equal("Blocked shop", source.Source);
                Assert.Equal(PriceScanStatus.Blocked, source.Status);
                Assert.Equal("This source is blocked.", source.Error);
            },
            source => Assert.Equal(PriceScanStatus.NotConfigured, source.Status));
        Assert.Empty(database.Context.PriceObservations);
    }

    [Fact]
    public async Task Scan_drops_non_usd_snippet_prices()
    {
        await using var database = await TestDatabase.CreateAsync();
        var watch = await SeedWatchAsync(database, priceAlertsEnabled: true);
        var scanner = Build(
            database,
            new StubSearch(Result("Omega Speedmaster — €4,200", "https://shop.example.test/omega")),
            new StubEbay(),
            new TestCatalog(new SiteCatalogEntry("Snippet shop", "shop.example.test")));

        var result = await scanner.ScanAsync(watch.Id, watch.UserId);

        Assert.Equal(PriceScanStatus.NoMatch, result!.Sources[0].Status);
        Assert.Empty(database.Context.PriceObservations);
        Assert.Empty(database.Context.PriceAlerts);
    }

    [Fact]
    public async Task Weak_match_is_stored_as_a_lead_but_can_never_create_an_alert()
    {
        await using var database = await TestDatabase.CreateAsync();
        var watch = await SeedWatchAsync(database, priceAlertsEnabled: true, target: 5_000m);
        var scanner = Build(
            database,
            new StubSearch(Result("Omega watch, new — $1,000", "https://shop.example.test/omega")),
            new StubEbay(),
            new TestCatalog(new SiteCatalogEntry("Snippet shop", "shop.example.test")));

        await scanner.ScanAsync(watch.Id, watch.UserId);

        var observation = await database.Context.PriceObservations.SingleAsync();
        Assert.Equal(PriceMatchConfidence.Low, observation.MatchConfidence);
        Assert.Empty(database.Context.PriceAlerts);
    }

    [Fact]
    public async Task High_confidence_price_below_target_and_previous_best_creates_each_alert_once()
    {
        await using var database = await TestDatabase.CreateAsync();
        var watch = await SeedWatchAsync(database, priceAlertsEnabled: true, target: 5_000m);
        var search = new StubSearch(
            Result("Omega Speedmaster Professional, pre-owned — $6,000", "https://shop.example.test/omega-1"),
            Result("Omega Speedmaster Professional, pre-owned — $4,000", "https://shop.example.test/omega-2"));
        var scanner = Build(
            database,
            search,
            new StubEbay(),
            new TestCatalog(new SiteCatalogEntry("Snippet shop", "shop.example.test")));

        var first = await scanner.ScanAsync(watch.Id, watch.UserId);
        var second = await scanner.ScanAsync(watch.Id, watch.UserId);

        Assert.Equal(0, first!.AlertsCreated);
        Assert.Equal(2, second!.AlertsCreated);
        var alerts = await database.Context.PriceAlerts
            .OrderBy(a => a.Trigger)
            .ToListAsync();
        Assert.Equal(
            [PriceAlertTrigger.BelowTarget, PriceAlertTrigger.NewBest],
            alerts.Select(a => a.Trigger));
        Assert.Equal(
            PriceObservationKind.Preowned,
            (await database.Context.PriceObservations
                .OrderByDescending(observation => observation.Price)
                .FirstAsync()).Kind);
    }

    [Fact]
    public async Task Repeated_scan_deduplicates_the_same_listing_and_price()
    {
        await using var database = await TestDatabase.CreateAsync();
        var watch = await SeedWatchAsync(database, priceAlertsEnabled: false);
        var scanner = Build(
            database,
            new StubSearch(Result("Omega Speedmaster, new — $4,500", "https://shop.example.test/omega")),
            new StubEbay(),
            new TestCatalog(new SiteCatalogEntry("Snippet shop", "shop.example.test")));

        var first = await scanner.ScanAsync(watch.Id, watch.UserId);
        var second = await scanner.ScanAsync(watch.Id, watch.UserId);

        Assert.Equal(1, first!.ObservationsAdded);
        Assert.Equal(0, second!.ObservationsAdded);
        Assert.Single(database.Context.PriceObservations);
    }

    [Fact]
    public async Task Changed_price_on_the_same_listing_is_retained_as_a_later_observation()
    {
        await using var database = await TestDatabase.CreateAsync();
        var watch = await SeedWatchAsync(database, priceAlertsEnabled: false);
        var scanner = Build(
            database,
            new StubSearch(
                Result("Omega Speedmaster, new — $4,500", "https://shop.example.test/omega"),
                Result("Omega Speedmaster, new — $4,200", "https://shop.example.test/omega")),
            new StubEbay(),
            new TestCatalog(new SiteCatalogEntry("Snippet shop", "shop.example.test")));

        await scanner.ScanAsync(watch.Id, watch.UserId);
        await scanner.ScanAsync(watch.Id, watch.UserId);

        Assert.Equal(
            [4_200m, 4_500m],
            await database.Context.PriceObservations
                .OrderBy(observation => observation.Price)
                .Select(observation => observation.Price)
                .ToListAsync());
    }

    [Fact]
    public async Task Scheduled_scan_only_considers_opted_in_wish_list_watches_that_are_due()
    {
        await using var database = await TestDatabase.CreateAsync();
        var enabled = await SeedWatchAsync(database, priceAlertsEnabled: true);
        var disabled = await SeedWatchAsync(
            database,
            priceAlertsEnabled: false,
            model: "Seamaster");
        var nonWishlist = await SeedWatchAsync(
            database,
            priceAlertsEnabled: true,
            model: "Constellation",
            isWishList: false);
        var search = new StubSearch(Result(
            "Omega Speedmaster, new — $4,500",
            "https://shop.example.test/omega"));
        var scanner = Build(
            database,
            search,
            new StubEbay(),
            new TestCatalog(new SiteCatalogEntry("Snippet shop", "shop.example.test")));

        var scanned = await scanner.ScanDueAsync();
        await database.Context.Entry(enabled).ReloadAsync();
        await database.Context.Entry(disabled).ReloadAsync();
        await database.Context.Entry(nonWishlist).ReloadAsync();

        Assert.Equal(1, scanned);
        Assert.Equal(1, search.Calls);
        Assert.NotNull(enabled.PriceCheckedAt);
        Assert.Null(disabled.PriceCheckedAt);
        Assert.Null(nonWishlist.PriceCheckedAt);
    }

    [Fact]
    public async Task Ownership_and_wish_list_guards_hide_and_refuse_unavailable_watches()
    {
        await using var database = await TestDatabase.CreateAsync();
        var ownerWatch = await SeedWatchAsync(database, priceAlertsEnabled: false);
        var other = TestDatabase.User("other");
        database.Context.Users.Add(other);
        await database.Context.SaveChangesAsync();
        var nonWishlist = new Watch
        {
            UserId = ownerWatch.UserId,
            Brand = "Omega",
            Model = "De Ville",
            IsWishList = false
        };
        database.Context.Watches.Add(nonWishlist);
        await database.Context.SaveChangesAsync();
        var scanner = Build(
            database,
            new StubSearch(),
            new StubEbay(),
            new TestCatalog(new SiteCatalogEntry("Snippet shop", "shop.example.test")));

        Assert.Null(await scanner.ScanAsync(ownerWatch.Id, other.Id));
        Assert.Null(await scanner.GetObservationsAsync(ownerWatch.Id, other.Id));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            scanner.ScanAsync(nonWishlist.Id, ownerWatch.UserId));
    }

    [Fact]
    public async Task Alert_reads_are_scoped_to_the_owner()
    {
        await using var database = await TestDatabase.CreateAsync();
        var watch = await SeedWatchAsync(database, priceAlertsEnabled: true);
        var other = TestDatabase.User("other");
        database.Context.Users.Add(other);
        var observation = new PriceObservation
        {
            WatchId = watch.Id,
            UserId = watch.UserId,
            Source = "Snippet shop",
            ListingKey = "A".PadLeft(64, 'A'),
            ListingUrl = "https://shop.example.test/omega",
            ListingTitle = "Omega Speedmaster",
            Price = 4_000m,
            Currency = "USD",
            MatchConfidence = PriceMatchConfidence.High,
            ObservedAt = DateTime.UtcNow,
            ObservedOnUtc = DateOnly.FromDateTime(DateTime.UtcNow)
        };
        database.Context.PriceObservations.Add(observation);
        await database.Context.SaveChangesAsync();
        var alert = new PriceAlert
        {
            PriceObservationId = observation.Id,
            UserId = watch.UserId,
            Trigger = PriceAlertTrigger.BelowTarget
        };
        database.Context.PriceAlerts.Add(alert);
        await database.Context.SaveChangesAsync();
        var service = new PriceAlertService(
            database.Context,
            NullLogger<PriceAlertService>.Instance);

        Assert.Empty(await service.GetAlertsAsync(other.Id, false));
        Assert.False(await service.MarkReadAsync(alert.Id, other.Id));
        Assert.True(await service.MarkReadAsync(alert.Id, watch.UserId));
        Assert.True((await database.Context.PriceAlerts.SingleAsync()).IsRead);
    }

    private static WishlistPriceScanner Build(
        TestDatabase database,
        StubSearch search,
        StubEbay ebay,
        ISiteCatalog catalog)
    {
        var alerts = new PriceAlertService(database.Context, NullLogger<PriceAlertService>.Instance);
        return new WishlistPriceScanner(
            database.Context,
            new StubSettings(),
            [search],
            ebay,
            catalog,
            alerts,
            NullLogger<WishlistPriceScanner>.Instance);
    }

    private static WebSearchResultItem Result(string title, string url) =>
        new(title, "", url, DateTime.UtcNow);

    private static async Task<Watch> SeedWatchAsync(
        TestDatabase database,
        bool priceAlertsEnabled,
        decimal? target = null,
        string model = "Speedmaster",
        bool isWishList = true)
    {
        var user = await database.Context.Users.FirstOrDefaultAsync()
            ?? TestDatabase.User("owner");
        if (user.Id == 0)
        {
            database.Context.Users.Add(user);
            await database.Context.SaveChangesAsync();
        }

        var watch = new Watch
        {
            UserId = user.Id,
            Brand = "Omega",
            Model = model,
            PurchasePrice = 5_000m,
            IsWishList = isWishList,
            PriceAlertEnabled = priceAlertsEnabled,
            PriceAlertTarget = target
        };
        database.Context.Watches.Add(watch);
        await database.Context.SaveChangesAsync();
        return watch;
    }

    private sealed class TestCatalog(params SiteCatalogEntry[] sites) : ISiteCatalog
    {
        public IReadOnlyList<SiteCatalogEntry> Sites => sites;
    }

    private sealed class StubSearch(params WebSearchResultItem[] items) : IWebSearchClient
    {
        public string ProviderName => "Brave";
        public int Calls { get; private set; }
        public WebSearchStatus Status { get; set; } = WebSearchStatus.Success;

        public Task<WebSearchResult> SearchAsync(string query, CancellationToken ct = default)
        {
            Calls++;
            if (Status != WebSearchStatus.Success)
                return Task.FromResult(new WebSearchResult(Status, [], "Not configured."));

            return Task.FromResult(new WebSearchResult(
                WebSearchStatus.Success,
                items.Length > 0 ? [items[Math.Min(Calls - 1, items.Length - 1)]] : [],
                null));
        }
    }

    private sealed class StubEbay : IEbayBrowseClient
    {
        public string ProviderName => "eBay";
        public MarketplaceSearchStatus Status { get; set; } = MarketplaceSearchStatus.NotConfigured;

        public Task<MarketplaceSearchResult> SearchAsync(string query, CancellationToken ct = default) =>
            Task.FromResult(new MarketplaceSearchResult(Status, [], "eBay is not configured."));
    }

    private sealed class StubSettings : IAppSettingsService
    {
        public Task<string> GetAsync(string key, string defaultValue = "") =>
            Task.FromResult(key == AppSettingsService.Keys.WebSearchProvider ? "Brave" : defaultValue);

        public Task<int> GetIntAsync(string key, int defaultValue) =>
            Task.FromResult(key == AppSettingsService.Keys.PriceAlertScanIntervalHours ? 24 : defaultValue);

        public Task SetAsync(string key, string value) => Task.CompletedTask;
        public Task<Dictionary<string, string>> GetAllAsync() =>
            Task.FromResult<Dictionary<string, string>>([]);
    }
}
