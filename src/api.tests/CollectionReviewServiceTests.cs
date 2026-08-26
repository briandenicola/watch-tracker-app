using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using WatchTracker.Api.Models;
using WatchTracker.Api.Services;

namespace WatchTracker.Api.Tests;

public class CollectionReviewServiceTests
{
    [Fact]
    public async Task Review_needs_at_least_two_watches()
    {
        await using var database = await TestDatabase.CreateAsync();
        var user = await AddUserAsync(database);
        await AddWatchAsync(database, user, "Seiko", "SKX007");
        var service = Build(database, ReplyWith(new { summary = "Fine." }));

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.GenerateAsync(user.Id));

        Assert.Contains("at least 2 watches", error.Message);
    }

    [Fact]
    public async Task Review_reports_when_ollama_is_not_configured()
    {
        await using var database = await TestDatabase.CreateAsync();
        var user = await AddUserAsync(database);
        await AddWatchAsync(database, user, "Seiko", "SKX007");
        await AddWatchAsync(database, user, "Omega", "Speedmaster");
        var service = Build(
            database,
            ReplyWith(new { summary = "Fine." }),
            new StubSettings(model: ""));

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.GenerateAsync(user.Id));

        Assert.Contains("Ollama", error.Message);
    }

    [Fact]
    public async Task Watch_ids_the_model_invented_are_dropped()
    {
        await using var database = await TestDatabase.CreateAsync();
        var user = await AddUserAsync(database);
        var owned = await AddWatchAsync(database, user, "Seiko", "SKX007");
        var wanted = await AddWatchAsync(database, user, "Omega", "Speedmaster", isWishList: true);
        var service = Build(database, ReplyWith(new
        {
            summary = "A focused collection.",
            strengths = new[]
            {
                new
                {
                    summary = "Solid diver",
                    detail = "The SKX is a genuine tool watch.",
                    // 9999 belongs to nobody; 4242 to no watch at all.
                    watchIds = new[] { owned.Id, 9999, wanted.Id, 4242 }
                }
            }
        }));

        var review = (await service.GenerateAsync(user.Id)).Review!;

        var strength = Assert.Single(review.Strengths);
        Assert.Equal(new[] { owned.Id, wanted.Id }, strength.WatchIds);
    }

    [Fact]
    public async Task Another_users_watch_cannot_be_cited()
    {
        await using var database = await TestDatabase.CreateAsync();
        var user = await AddUserAsync(database);
        var stranger = await AddUserAsync(database, "stranger");
        await AddWatchAsync(database, user, "Seiko", "SKX007");
        await AddWatchAsync(database, user, "Omega", "Speedmaster");
        var theirs = await AddWatchAsync(database, stranger, "Rolex", "Submariner");
        var service = Build(database, ReplyWith(new
        {
            summary = "Mixed.",
            weaknesses = new[]
            {
                new { summary = "Overlap", detail = "Similar divers.", watchIds = new[] { theirs.Id } }
            }
        }));

        var review = (await service.GenerateAsync(user.Id)).Review!;

        Assert.Empty(Assert.Single(review.Weaknesses).WatchIds);
    }

    [Fact]
    public async Task Findings_are_capped_per_section()
    {
        await using var database = await TestDatabase.CreateAsync();
        var user = await AddUserAsync(database);
        await AddWatchAsync(database, user, "Seiko", "SKX007");
        await AddWatchAsync(database, user, "Omega", "Speedmaster");
        var many = Enumerable.Range(0, 20)
            .Select(i => new { summary = $"Point {i}", detail = "Detail.", watchIds = Array.Empty<int>() })
            .ToArray();
        var service = Build(database, ReplyWith(new { summary = "Long.", strengths = many }));

        var review = (await service.GenerateAsync(user.Id)).Review!;

        Assert.Equal(6, review.Strengths.Count);
    }

    [Fact]
    public async Task An_empty_reply_is_rejected_rather_than_stored()
    {
        await using var database = await TestDatabase.CreateAsync();
        var user = await AddUserAsync(database);
        await AddWatchAsync(database, user, "Seiko", "SKX007");
        await AddWatchAsync(database, user, "Omega", "Speedmaster");
        var service = Build(database, ReplyWith(new { summary = "Nothing to say." }));

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GenerateAsync(user.Id));
        Assert.Empty(database.Context.CollectionReviews);
    }

    [Fact]
    public async Task Prose_around_the_json_is_tolerated()
    {
        await using var database = await TestDatabase.CreateAsync();
        var user = await AddUserAsync(database);
        await AddWatchAsync(database, user, "Seiko", "SKX007");
        await AddWatchAsync(database, user, "Omega", "Speedmaster");
        var padded = "Here is your review:\n```json\n"
            + """{"summary":"Good.","strengths":[{"summary":"Range","detail":"Varied.","watchIds":[]}]}"""
            + "\n```";
        var service = Build(database, new StubHandler(OllamaBody(padded)));

        var review = (await service.GenerateAsync(user.Id)).Review!;

        Assert.Equal("Good.", review.Summary);
        Assert.Single(review.Strengths);
    }

    [Fact]
    public async Task State_reports_an_unconfigured_ollama_before_anything_is_clicked()
    {
        await using var database = await TestDatabase.CreateAsync();
        var user = await AddUserAsync(database);
        var service = Build(
            database,
            ReplyWith(new { summary = "Fine." }),
            new StubSettings(model: ""));

        var state = await service.GetStateAsync(user.Id);

        Assert.False(state.Configured);
        Assert.Contains("Ollama", state.ConfigurationHint!);
        Assert.Null(state.Review);
    }

    [Fact]
    public async Task State_is_configured_when_ollama_is_set_up()
    {
        await using var database = await TestDatabase.CreateAsync();
        var user = await AddUserAsync(database);
        var service = Build(database, ReplyWith(new { summary = "Fine." }));

        var state = await service.GetStateAsync(user.Id);

        Assert.True(state.Configured);
        Assert.Null(state.ConfigurationHint);
    }

    [Fact]
    public async Task Latest_is_null_until_a_review_has_run()
    {
        await using var database = await TestDatabase.CreateAsync();
        var user = await AddUserAsync(database);
        var service = Build(database, ReplyWith(new { summary = "Fine." }));

        Assert.Null((await service.GetStateAsync(user.Id)).Review);
    }

    [Fact]
    public async Task A_fresh_review_is_not_stale_but_a_new_watch_makes_it_so()
    {
        await using var database = await TestDatabase.CreateAsync();
        var user = await AddUserAsync(database);
        await AddWatchAsync(database, user, "Seiko", "SKX007");
        await AddWatchAsync(database, user, "Omega", "Speedmaster");
        var service = Build(database, ReplyWith(new
        {
            summary = "Good.",
            strengths = new[] { new { summary = "Range", detail = "Varied.", watchIds = Array.Empty<int>() } }
        }));

        await service.GenerateAsync(user.Id);
        Assert.False((await service.GetStateAsync(user.Id)).Review!.IsStale);

        await AddWatchAsync(database, user, "Tudor", "Black Bay");
        Assert.True((await service.GetStateAsync(user.Id)).Review!.IsStale);
    }

    [Fact]
    public async Task Regenerating_replaces_the_stored_review()
    {
        await using var database = await TestDatabase.CreateAsync();
        var user = await AddUserAsync(database);
        await AddWatchAsync(database, user, "Seiko", "SKX007");
        await AddWatchAsync(database, user, "Omega", "Speedmaster");
        var service = Build(database, new StubHandler(
            OllamaBody("""{"summary":"First.","strengths":[{"summary":"A","detail":"a","watchIds":[]}]}"""),
            OllamaBody("""{"summary":"Second.","strengths":[{"summary":"B","detail":"b","watchIds":[]}]}""")));

        await service.GenerateAsync(user.Id);
        var second = (await service.GenerateAsync(user.Id)).Review!;

        Assert.Equal("Second.", second.Summary);
        Assert.Single(database.Context.CollectionReviews);
    }

    [Fact]
    public async Task Facts_separate_the_collection_from_the_wish_list()
    {
        await using var database = await TestDatabase.CreateAsync();
        var user = await AddUserAsync(database);
        await AddWatchAsync(database, user, "Seiko", "SKX007");
        await AddWatchAsync(database, user, "Seiko", "SKX009");
        await AddWatchAsync(database, user, "Omega", "Speedmaster", isWishList: true);
        var profile = new CollectionProfileService(database.Context);

        var facts = await profile.GetReviewFactsAsync(user.Id);

        Assert.Equal(2, facts.Collection.WatchCount);
        Assert.Equal(1, facts.Wishlist.WatchCount);
        Assert.Equal(3, facts.Combined.WatchCount);
        Assert.Equal(2, facts.CollectionWatches.Count);
        Assert.Single(facts.WishlistWatches);
        Assert.Single(facts.WishlistFit);
    }

    [Fact]
    public async Task Facts_flag_a_wish_list_watch_already_owned()
    {
        await using var database = await TestDatabase.CreateAsync();
        var user = await AddUserAsync(database);
        var owned = await AddWatchAsync(database, user, "Seiko", "SKX007");
        var wanted = await AddWatchAsync(database, user, "seiko", "skx007", isWishList: true);
        var profile = new CollectionProfileService(database.Context);

        var facts = await profile.GetReviewFactsAsync(user.Id);

        var overlap = Assert.Single(facts.WishlistOverlaps);
        Assert.Equal(wanted.Id, overlap.WishlistWatchId);
        Assert.Equal(owned.Id, Assert.Single(overlap.CollectionWatchIds));
    }

    [Fact]
    public async Task Wish_list_redundancy_is_not_described_as_active_watches()
    {
        await using var database = await TestDatabase.CreateAsync();
        var user = await AddUserAsync(database);
        await AddWatchAsync(database, user, "Omega", "Speedmaster", isWishList: true);
        await AddWatchAsync(database, user, "Omega", "Speedmaster", isWishList: true);
        var profile = new CollectionProfileService(database.Context);

        var facts = await profile.GetReviewFactsAsync(user.Id);

        var redundancy = Assert.Single(facts.Wishlist.Redundancies);
        Assert.Contains("wish list watches", redundancy.Reason);
        Assert.DoesNotContain("active watches", redundancy.Reason);
    }

    private static CollectionReviewService Build(
        TestDatabase database,
        HttpMessageHandler handler,
        IAppSettingsService? settings = null) =>
        new(
            database.Context,
            settings ?? new StubSettings(),
            new CollectionProfileService(database.Context),
            new HttpClient(handler),
            NullLogger<CollectionReviewService>.Instance);

    private static StubHandler ReplyWith(object payload) =>
        new(OllamaBody(JsonSerializer.Serialize(payload)));

    private static string OllamaBody(string content) =>
        JsonSerializer.Serialize(new { message = new { content } });

    private static async Task<User> AddUserAsync(TestDatabase database, string name = "owner")
    {
        var user = TestDatabase.User(name);
        database.Context.Add(user);
        await database.Context.SaveChangesAsync();
        return user;
    }

    private static async Task<Watch> AddWatchAsync(
        TestDatabase database,
        User user,
        string brand,
        string model,
        bool isWishList = false)
    {
        var watch = new Watch
        {
            Brand = brand,
            Model = model,
            IsWishList = isWishList,
            UserId = user.Id
        };
        database.Context.Add(watch);
        await database.Context.SaveChangesAsync();
        return watch;
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

    private sealed class StubSettings(string model = "test-model") : IAppSettingsService
    {
        public Task<string> GetAsync(string key, string defaultValue = "") =>
            Task.FromResult(key switch
            {
                AppSettingsService.Keys.OllamaUrl => "http://ollama.test",
                AppSettingsService.Keys.OllamaModel => model,
                _ => defaultValue
            });

        public Task<int> GetIntAsync(string key, int defaultValue) => Task.FromResult(defaultValue);
        public Task SetAsync(string key, string value) => Task.CompletedTask;
        public Task<Dictionary<string, string>> GetAllAsync() =>
            Task.FromResult<Dictionary<string, string>>([]);
    }
}
