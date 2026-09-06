using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using WatchTracker.Api.Data;
using WatchTracker.Api.DTOs;
using WatchTracker.Api.Models;
using WatchTracker.Api.Services;

namespace WatchTracker.Api.Tests;

public class CollectionReviewCandidateServiceTests
{
    [Fact]
    public async Task Candidates_come_from_listings_a_marketplace_actually_returned()
    {
        await using var database = await TestDatabase.CreateAsync();
        var user = await SeedReviewedCollectionAsync(database);
        var market = new StubMarketplace(Listing("tudor-1", "Tudor Black Bay 36 automatic", 2400m));
        var service = Build(database, market, Queries(), Picks(("eBay", "tudor-1", "Fills the sub-38mm gap.")));

        var result = await service.GenerateAsync(user.Id, new GenerateCandidatesDto());

        var candidate = Assert.Single(result.Candidates);
        Assert.Equal("tudor-1", candidate.ProviderItemId);
        Assert.Equal(2400m, candidate.Price);
        // The model's line leads, the computed reasons follow it.
        Assert.Equal("Fills the sub-38mm gap.", candidate.Reasons[0]);
        Assert.True(candidate.Reasons.Count > 1);
        Assert.NotNull(candidate.FitScore);
    }

    [Fact]
    public async Task A_listing_no_marketplace_returned_cannot_become_a_candidate()
    {
        await using var database = await TestDatabase.CreateAsync();
        var user = await SeedReviewedCollectionAsync(database);
        var market = new StubMarketplace(Listing("tudor-1", "Tudor Black Bay 36 automatic", 2400m));
        var service = Build(
            database,
            market,
            Queries(),
            // The second id was never in any result: an invented watch.
            Picks(("eBay", "tudor-1", "Real."), ("eBay", "rolex-daytona", "Invented.")));

        var result = await service.GenerateAsync(user.Id, new GenerateCandidatesDto());

        Assert.Single(result.Candidates);
        Assert.Equal("tudor-1", result.Candidates[0].ProviderItemId);
    }

    [Fact]
    public async Task The_model_can_search_again_when_the_first_round_missed()
    {
        await using var database = await TestDatabase.CreateAsync();
        var user = await SeedReviewedCollectionAsync(database);
        var market = new StubMarketplace(
            Listing("tudor-1", "Tudor Black Bay 36 automatic", 2400m),
            Listing("cartier-1", "Cartier Tank Must small", 2900m));
        // Round one asks for Tudor, round two changes its mind and asks for Cartier,
        // round three picks from everything seen.
        var service = Build(
            database,
            market,
            Reply(new { queries = new[] { new { brand = "Tudor", model = "Black Bay" } } }),
            Reply(new { queries = new[] { new { brand = "Cartier", model = "Tank" } } }),
            Picks(("eBay", "cartier-1", "The dressy gap, answered.")));

        var result = await service.GenerateAsync(user.Id, new GenerateCandidatesDto());

        Assert.Equal(2, market.Searches.Count);
        Assert.Contains("Cartier", market.Searches[1]);
        Assert.Equal("cartier-1", Assert.Single(result.Candidates).ProviderItemId);
    }

    [Fact]
    public async Task Listings_that_do_not_name_the_watch_are_filtered_out()
    {
        await using var database = await TestDatabase.CreateAsync();
        var user = await SeedReviewedCollectionAsync(database);
        // The noise a marketplace returns for any watch query.
        var market = new StubMarketplace(
            Listing("strap-1", "Leather strap for Tudor watches 20mm", 39m),
            Listing("tudor-1", "Tudor Black Bay 36 automatic", 2400m));
        var service = Build(
            database,
            market,
            Queries(),
            Picks(("eBay", "strap-1", "A strap."), ("eBay", "tudor-1", "The watch.")));

        var result = await service.GenerateAsync(user.Id, new GenerateCandidatesDto());

        Assert.Equal("tudor-1", Assert.Single(result.Candidates).ProviderItemId);
    }

    [Fact]
    public async Task An_auction_is_not_a_candidate()
    {
        await using var database = await TestDatabase.CreateAsync();
        var user = await SeedReviewedCollectionAsync(database);
        var auction = Listing("tudor-1", "Tudor Black Bay 36 automatic", 2400m) with
        {
            ListingType = MarketplaceListingType.Auction
        };
        var service = Build(database, new StubMarketplace(auction), Queries(), Picks(("eBay", "tudor-1", "No.")));

        var result = await service.GenerateAsync(user.Id, new GenerateCandidatesDto());

        Assert.Empty(result.Candidates);
    }

    [Fact]
    public async Task An_unconfigured_marketplace_is_reported_rather_than_read_as_nothing_found()
    {
        await using var database = await TestDatabase.CreateAsync();
        var user = await SeedReviewedCollectionAsync(database);
        var market = new StubMarketplace { Status = MarketplaceSearchStatus.NotConfigured };
        var service = Build(database, market, Queries(), Picks());

        var result = await service.GenerateAsync(user.Id, new GenerateCandidatesDto());

        Assert.Empty(result.Candidates);
        var status = Assert.Single(result.MarketplaceStatus);
        Assert.Equal("NotConfigured", status.Status);
        Assert.Equal("eBay", status.Provider);
    }

    [Fact]
    public async Task A_provider_error_is_reported_with_its_reason()
    {
        await using var database = await TestDatabase.CreateAsync();
        var user = await SeedReviewedCollectionAsync(database);
        var market = new StubMarketplace
        {
            Status = MarketplaceSearchStatus.ProviderError,
            Error = "eBay returned 503."
        };
        var service = Build(database, market, Queries(), Picks());

        var result = await service.GenerateAsync(user.Id, new GenerateCandidatesDto());

        var status = Assert.Single(result.MarketplaceStatus);
        Assert.Equal("ProviderError", status.Status);
        Assert.Equal("eBay returned 503.", status.Error);
    }

    [Fact]
    public async Task Candidates_need_a_review_to_be_found_against()
    {
        await using var database = await TestDatabase.CreateAsync();
        var user = TestDatabase.User("owner");
        database.Context.Users.Add(user);
        await database.Context.SaveChangesAsync();
        var service = Build(database, new StubMarketplace(), Queries(), Picks());

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.GenerateAsync(user.Id, new GenerateCandidatesDto()));

        Assert.Contains("Run a collection review first", error.Message);
    }

    [Fact]
    public async Task A_budget_without_a_currency_is_refused()
    {
        await using var database = await TestDatabase.CreateAsync();
        var user = await SeedReviewedCollectionAsync(database);
        var service = Build(database, new StubMarketplace(), Queries(), Picks());

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.GenerateAsync(user.Id, new GenerateCandidatesDto { Budget = 3000m }));

        Assert.Contains("currency", error.Message);
    }

    [Fact]
    public async Task Candidates_over_the_requested_budget_are_excluded()
    {
        await using var database = await TestDatabase.CreateAsync();
        var user = await SeedReviewedCollectionAsync(database);
        var market = new StubMarketplace(
            Listing("within-budget", "Tudor Black Bay automatic", 1900m),
            Listing("over-budget", "Tudor Black Bay automatic", 2400m));
        var service = Build(
            database,
            market,
            Queries(),
            Picks(
                ("eBay", "within-budget", "Within budget."),
                ("eBay", "over-budget", "Over budget.")));

        var result = await service.GenerateAsync(
            user.Id,
            new GenerateCandidatesDto { Budget = 2000m, Currency = "USD" });

        var candidate = Assert.Single(result.Candidates);
        Assert.Equal("within-budget", candidate.ProviderItemId);
    }

    [Fact]
    public async Task Stale_listings_are_dropped_on_read()
    {
        await using var database = await TestDatabase.CreateAsync();
        var user = await SeedReviewedCollectionAsync(database);
        var fresh = Listing("tudor-1", "Tudor Black Bay 36 automatic", 2400m);
        // Same search, seen nine days ago: likely sold by now.
        var old = Listing("tudor-2", "Tudor Black Bay 36 blue", 2350m) with
        {
            ObservedAt = DateTime.UtcNow - TimeSpan.FromDays(9)
        };
        var service = Build(
            database,
            new StubMarketplace(fresh, old),
            Queries(),
            Picks(("eBay", "tudor-1", "Fresh."), ("eBay", "tudor-2", "Stale.")));

        await service.GenerateAsync(user.Id, new GenerateCandidatesDto());
        var stored = await database.Context.CollectionReviews.SingleAsync();
        var read = CollectionReviewCandidateService.Read(stored);

        Assert.Equal("tudor-1", Assert.Single(read.Candidates).ProviderItemId);
        Assert.True(read.DroppedStaleListings);
    }

    [Fact]
    public async Task A_new_report_clears_candidates_found_against_the_old_gaps()
    {
        await using var database = await TestDatabase.CreateAsync();
        var user = await SeedReviewedCollectionAsync(database);
        var service = Build(
            database,
            new StubMarketplace(Listing("tudor-1", "Tudor Black Bay 36 automatic", 2400m)),
            Queries(),
            Picks(("eBay", "tudor-1", "Fills the gap.")));
        await service.GenerateAsync(user.Id, new GenerateCandidatesDto());

        await ReviewServiceFor(database).GenerateAsync(user.Id);

        var stored = await database.Context.CollectionReviews.SingleAsync();
        Assert.Null(stored.CandidatesJson);
        Assert.Null(stored.CandidatesGeneratedAt);
    }

    [Fact]
    public async Task A_stored_candidate_can_be_added_to_the_wish_list()
    {
        await using var database = await TestDatabase.CreateAsync();
        var user = await SeedReviewedCollectionAsync(database);
        var service = Build(
            database,
            new StubMarketplace(Listing("tudor-1", "Tudor Black Bay 36 automatic", 2400m)),
            Queries(),
            Picks(("eBay", "tudor-1", "Fills the gap.")));
        await service.GenerateAsync(user.Id, new GenerateCandidatesDto());

        var result = await service.AddToWishlistAsync(
            user.Id,
            new CandidateWishlistActionDto { Provider = "eBay", ProviderItemId = "tudor-1" });

        Assert.True(result!.Added);
        var added = await database.Context.Watches.SingleAsync(w => w.Id == result.WatchId);
        Assert.True(added.IsWishList);
        Assert.Equal("tudor-1", added.MarketplaceItemId);
        Assert.Equal("Added from a collection review candidate.", added.Notes);
    }

    [Fact]
    public async Task A_candidate_that_was_never_stored_cannot_be_added()
    {
        await using var database = await TestDatabase.CreateAsync();
        var user = await SeedReviewedCollectionAsync(database);
        var service = Build(
            database,
            new StubMarketplace(Listing("tudor-1", "Tudor Black Bay 36 automatic", 2400m)),
            Queries(),
            Picks(("eBay", "tudor-1", "Fills the gap.")));
        await service.GenerateAsync(user.Id, new GenerateCandidatesDto());

        var result = await service.AddToWishlistAsync(
            user.Id,
            new CandidateWishlistActionDto { Provider = "eBay", ProviderItemId = "someone-elses-listing" });

        Assert.Null(result);
    }

    [Fact]
    public async Task An_unreachable_model_is_reported_as_a_request_failure_and_logged()
    {
        await using var database = await TestDatabase.CreateAsync();
        var user = await SeedReviewedCollectionAsync(database);
        var logger = new CollectingLogger<CollectionReviewCandidateService>();
        var service = new CollectionReviewCandidateService(
            database.Context,
            new StubSettings(),
            new CollectionProfileService(database.Context),
            new RecommendationWishlistService(database.Context),
            [new StubMarketplace()],
            new HttpClient(new ThrowingHandler()),
            logger);

        // Unhandled, this left the caller with a bare 500 and the operator with a
        // stack trace that named no setting to fix.
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.GenerateAsync(user.Id, new GenerateCandidatesDto()));

        Assert.Contains("could not reach Ollama", error.Message);
        Assert.Contains(
            logger.Messages,
            message => message.Contains(
                "could not reach the configured model provider",
                StringComparison.OrdinalIgnoreCase));
    }

    private sealed class ThrowingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            throw new HttpRequestException("Connection refused");
    }

    private sealed class CollectingLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            Messages.Add(formatter(state, exception));
    }

    private static CollectionReviewCandidateService Build(
        TestDatabase database,
        StubMarketplace marketplace,
        params string[] replies) =>
        new(
            database.Context,
            new StubSettings(),
            new CollectionProfileService(database.Context),
            new RecommendationWishlistService(database.Context),
            [marketplace],
            new HttpClient(new StubHandler(replies)),
            NullLogger<CollectionReviewCandidateService>.Instance);

    private static CollectionReviewService ReviewServiceFor(TestDatabase database) =>
        new(
            database.Context,
            new StubSettings(),
            new CollectionProfileService(database.Context),
            new HttpClient(new StubHandler(Reply(new
            {
                summary = "Regenerated.",
                strengths = new[] { new { summary = "Still fine", detail = "As before.", watchIds = Array.Empty<int>() } }
            }))),
            NullLogger<CollectionReviewService>.Instance);

    private static string Queries() =>
        Reply(new { queries = new[] { new { brand = "Tudor", model = "Black Bay" } } });

    private static string Picks(params (string Provider, string ItemId, string Rationale)[] picks) =>
        Reply(new
        {
            candidates = picks
                .Select(p => new { provider = p.Provider, providerItemId = p.ItemId, rationale = p.Rationale })
                .ToArray()
        });

    private static string Reply(object payload) =>
        JsonSerializer.Serialize(new
        {
            message = new { content = JsonSerializer.Serialize(payload) }
        });

    private static MarketplaceListingItem Listing(string itemId, string title, decimal price) => new(
        Provider: "eBay",
        ProviderItemId: itemId,
        Title: title,
        ItemUrl: $"https://example.test/{itemId}",
        ImageUrl: null,
        Price: price,
        ShippingPrice: 0m,
        TotalPrice: price,
        Currency: "USD",
        ListingType: MarketplaceListingType.FixedPrice,
        Condition: "Pre-owned",
        SellerName: "seller",
        SellerFeedbackPercent: 99.2m,
        ObservedAt: DateTime.UtcNow);

    /// <summary>A collection with a report already stored against it.</summary>
    private static async Task<User> SeedReviewedCollectionAsync(TestDatabase database)
    {
        var user = TestDatabase.User("owner");
        database.Context.Users.Add(user);
        await database.Context.SaveChangesAsync();

        foreach (var (brand, model, size) in new[]
        {
            ("Seiko", "SKX007", 42.5),
            ("Seiko", "SKX009", 42.5),
            ("Omega", "Speedmaster", 42.0)
        })
        {
            database.Context.Watches.Add(new Watch
            {
                UserId = user.Id,
                Brand = brand,
                Model = model,
                MovementType = MovementType.Automatic,
                CaseSizeMm = size,
                DialColor = "Black",
                BandType = "Steel"
            });
        }
        await database.Context.SaveChangesAsync();

        await ReviewServiceFor(database).GenerateAsync(user.Id);
        return user;
    }

    private sealed class StubMarketplace(params MarketplaceListingItem[] listings) : IMarketplaceSearchClient
    {
        public string ProviderName => "eBay";
        public MarketplaceSearchStatus Status { get; set; } = MarketplaceSearchStatus.Success;
        public string? Error { get; set; }
        public List<string> Searches { get; } = [];

        public Task<MarketplaceSearchResult> SearchAsync(string query, CancellationToken ct = default)
        {
            Searches.Add(query);
            return Task.FromResult(new MarketplaceSearchResult(
                Status,
                Status == MarketplaceSearchStatus.Success ? listings : [],
                Error));
        }
    }

    private sealed class StubHandler(params string[] bodies) : HttpMessageHandler
    {
        private int calls;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var body = bodies[Math.Min(calls, bodies.Length - 1)];
            calls++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body)
            });
        }
    }

    private sealed class StubSettings : IAppSettingsService
    {
        public Task<string> GetAsync(string key, string defaultValue = "") =>
            Task.FromResult(key switch
            {
                AppSettingsService.Keys.OllamaUrl => "http://ollama.test",
                AppSettingsService.Keys.OllamaModel => "test-model",
                _ => defaultValue
            });

        public Task<int> GetIntAsync(string key, int defaultValue) => Task.FromResult(defaultValue);
        public Task SetAsync(string key, string value) => Task.CompletedTask;
        public Task<Dictionary<string, string>> GetAllAsync() =>
            Task.FromResult<Dictionary<string, string>>([]);
    }
}
