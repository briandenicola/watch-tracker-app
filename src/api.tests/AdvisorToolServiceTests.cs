using System.Text.Json;
using WatchTracker.Api.DTOs;
using WatchTracker.Api.Services;

namespace WatchTracker.Api.Tests;

public class AdvisorToolServiceTests
{
    [Fact]
    public async Task Marketplace_results_are_bounded_by_budget_and_scored_from_observed_price()
    {
        await using var database = await TestDatabase.CreateAsync();
        var user = TestDatabase.User("owner");
        database.Context.Users.Add(user);
        await database.Context.SaveChangesAsync();
        var profileService = new CollectionProfileService(database.Context);
        var profile = await profileService.GetProfileAsync(user.Id);
        var service = new AdvisorToolService(
            database.Context,
            profileService,
            [new StubMarketplaceClient()],
            [],
            new StubSettings());
        var context = new AdvisorToolContext(user.Id, profile);

        var search = await service.ExecuteAsync(
            "marketplace_search",
            JsonSerializer.SerializeToElement(new
            {
                query = "example watch",
                maxPrice = 1500,
                currency = "USD"
            }),
            context);
        var score = await service.ExecuteAsync(
            "score_listing",
            JsonSerializer.SerializeToElement(new
            {
                provider = "TestMarket",
                providerItemId = "within-budget",
                budget = 1500,
                currency = "USD"
            }),
            context);

        Assert.Contains("within-budget", search.OutputJson);
        Assert.DoesNotContain("over-budget", search.OutputJson);
        Assert.DoesNotContain("auction", search.OutputJson);
        Assert.DoesNotContain("other-currency", search.OutputJson);
        Assert.Contains("\"budgetFitScore\":100", score.OutputJson);
        Assert.Single(context.Listings);
        Assert.Single(context.ListingScores);
    }

    [Fact]
    public async Task Collection_tool_returns_only_the_current_users_active_collection()
    {
        await using var database = await TestDatabase.CreateAsync();
        var owner = TestDatabase.User("owner");
        var other = TestDatabase.User("other");
        database.Context.Users.AddRange(owner, other);
        await database.Context.SaveChangesAsync();
        database.Context.Watches.AddRange(
            new()
            {
                UserId = owner.Id,
                Brand = "Owned",
                Model = "Visible"
            },
            new()
            {
                UserId = other.Id,
                Brand = "Other",
                Model = "Hidden"
            });
        await database.Context.SaveChangesAsync();
        var profileService = new CollectionProfileService(database.Context);
        var profile = await profileService.GetProfileAsync(owner.Id);
        var service = new AdvisorToolService(
            database.Context,
            profileService,
            [],
            [],
            new StubSettings());

        var result = await service.ExecuteAsync(
            "collection_watches",
            JsonSerializer.SerializeToElement(new { }),
            new AdvisorToolContext(owner.Id, profile));

        Assert.Contains("Owned", result.OutputJson);
        Assert.DoesNotContain("Other", result.OutputJson);
    }

    [Fact]
    public async Task Resale_comparables_preserve_provider_failure_status()
    {
        await using var database = await TestDatabase.CreateAsync();
        var user = TestDatabase.User("owner");
        database.Context.Users.Add(user);
        await database.Context.SaveChangesAsync();
        var profileService = new CollectionProfileService(database.Context);
        var profile = await profileService.GetProfileAsync(user.Id);
        var service = new AdvisorToolService(
            database.Context,
            profileService,
            [new FailingMarketplaceClient()],
            [],
            new StubSettings());

        var result = await service.ExecuteAsync(
            "resale_comparables",
            JsonSerializer.SerializeToElement(new
            {
                brand = "Example",
                model = "Watch",
                currency = "USD"
            }),
            new AdvisorToolContext(user.Id, profile));

        Assert.Contains("\"status\":\"ProviderError\"", result.OutputJson);
        Assert.Contains("provider unavailable", result.OutputJson);
        Assert.Contains("\"count\":0", result.OutputJson);
        Assert.Equal("unavailable", result.Activity.Status);
        Assert.Contains("provider unavailable", result.Activity.Message);
    }

    [Fact]
    public async Task Marketplace_currency_filter_applies_without_a_budget()
    {
        await using var database = await TestDatabase.CreateAsync();
        var user = TestDatabase.User("owner");
        database.Context.Users.Add(user);
        await database.Context.SaveChangesAsync();
        var profileService = new CollectionProfileService(database.Context);
        var profile = await profileService.GetProfileAsync(user.Id);
        var service = new AdvisorToolService(
            database.Context,
            profileService,
            [new StubMarketplaceClient()],
            [],
            new StubSettings());

        var result = await service.ExecuteAsync(
            "marketplace_search",
            JsonSerializer.SerializeToElement(new
            {
                query = "example watch",
                currency = "EUR"
            }),
            new AdvisorToolContext(user.Id, profile));

        Assert.Contains("other-currency", result.OutputJson);
        Assert.DoesNotContain("within-budget", result.OutputJson);
    }

    private sealed class StubMarketplaceClient : IMarketplaceSearchClient
    {
        public string ProviderName => "TestMarket";

        public Task<MarketplaceSearchResult> SearchAsync(
            string query,
            CancellationToken ct = default)
        {
            var observedAt = DateTime.UtcNow;
            return Task.FromResult(new MarketplaceSearchResult(
                MarketplaceSearchStatus.Success,
                [
                    new MarketplaceListingItem(
                        ProviderName,
                        "within-budget",
                        "Example Watch",
                        "https://market.test/within",
                        null,
                        1000,
                        25,
                        1025,
                        "USD",
                        MarketplaceListingType.FixedPrice,
                        "Used",
                        "seller",
                        99,
                        observedAt),
                    new MarketplaceListingItem(
                        ProviderName,
                        "over-budget",
                        "Expensive Watch",
                        "https://market.test/over",
                        null,
                        2000,
                        25,
                        2025,
                        "USD",
                        MarketplaceListingType.FixedPrice,
                        "Used",
                        "seller",
                        99,
                        observedAt),
                    new MarketplaceListingItem(
                        ProviderName,
                        "auction",
                        "Auction Watch",
                        "https://market.test/auction",
                        null,
                        100,
                        0,
                        100,
                        "USD",
                        MarketplaceListingType.Auction,
                        "Used",
                        "seller",
                        99,
                        observedAt),
                    new MarketplaceListingItem(
                        ProviderName,
                        "other-currency",
                        "Euro Watch",
                        "https://market.test/euro",
                        null,
                        900,
                        0,
                        900,
                        "EUR",
                        MarketplaceListingType.FixedPrice,
                        "Used",
                        "seller",
                        99,
                        observedAt)
                ]));
        }

        private sealed class FailingMarketplaceClient : IMarketplaceSearchClient
        {
            public string ProviderName => "FailedMarket";

            public Task<MarketplaceSearchResult> SearchAsync(
                string query,
                CancellationToken ct = default) =>
                Task.FromResult(new MarketplaceSearchResult(
                    MarketplaceSearchStatus.ProviderError,
                    [],
                    "provider unavailable"));
        }
    }

    private sealed class StubSettings : IAppSettingsService
    {
        public Task<string> GetAsync(string key, string defaultValue = "") =>
            Task.FromResult(defaultValue);
        public Task<int> GetIntAsync(string key, int defaultValue) => Task.FromResult(defaultValue);
        public Task SetAsync(string key, string value) => Task.CompletedTask;
        public Task<Dictionary<string, string>> GetAllAsync() =>
            Task.FromResult<Dictionary<string, string>>([]);
    }
}
