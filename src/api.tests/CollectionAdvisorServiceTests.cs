using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using WatchTracker.Api.DTOs;
using WatchTracker.Api.Models;
using WatchTracker.Api.Services;

namespace WatchTracker.Api.Tests;

public class CollectionAdvisorServiceTests
{
    [Fact]
    public async Task Failed_reply_does_not_persist_either_turn()
    {
        await using var database = await TestDatabase.CreateAsync();
        var user = TestDatabase.User("owner");
        database.Context.Users.Add(user);
        await database.Context.SaveChangesAsync();
        var generator = new StubReplyGenerator { Failure = new InvalidOperationException("model failed") };
        var service = CreateService(database, generator);
        var session = await service.StartNewSessionAsync(user.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SendMessageAsync(
                session.Session.Id,
                user.Id,
                new SendAdvisorMessageDto { Message = "Help me" }));

        Assert.Empty(await database.Context.AdvisorMessages.ToListAsync());
    }

    [Fact]
    public async Task Successful_reply_persists_both_turns_and_structured_payload()
    {
        await using var database = await TestDatabase.CreateAsync();
        var user = TestDatabase.User("owner");
        database.Context.Users.Add(user);
        await database.Context.SaveChangesAsync();
        var generator = new StubReplyGenerator
        {
            Reply = new AdvisorGeneratedReply(
                "Answer",
                [
                    new AdvisorCitationDto
                    {
                        Title = "Source",
                        Url = "https://example.test",
                        Provider = "Test",
                        ObservedAt = DateTime.UtcNow
                    }
                ],
                [],
                ["What is your budget?"],
                [])
        };
        var service = CreateService(database, generator);
        var session = await service.StartNewSessionAsync(user.Id);

        var state = await service.SendMessageAsync(
            session.Session.Id,
            user.Id,
            new SendAdvisorMessageDto { Message = "Help me" });

        Assert.NotNull(state);
        Assert.Equal(2, state.Session.Messages.Count);
        Assert.Single(state.Session.Messages[1].Citations);
        Assert.Equal("What is your budget?", Assert.Single(state.Session.Messages[1].FollowUps));
    }

    [Fact]
    public async Task Session_access_is_scoped_to_its_owner()
    {
        await using var database = await TestDatabase.CreateAsync();
        var owner = TestDatabase.User("owner");
        var other = TestDatabase.User("other");
        database.Context.Users.AddRange(owner, other);
        await database.Context.SaveChangesAsync();
        var service = CreateService(database, new StubReplyGenerator());
        var session = await service.StartNewSessionAsync(owner.Id);

        var state = await service.GetStateAsync(session.Session.Id, other.Id);

        Assert.Null(state);
    }

    [Fact]
    public async Task Feedback_is_upserted_for_owned_persisted_recommendation()
    {
        await using var database = await TestDatabase.CreateAsync();
        var owner = TestDatabase.User("owner");
        var other = TestDatabase.User("other");
        database.Context.Users.AddRange(owner, other);
        await database.Context.SaveChangesAsync();
        var service = CreateService(database, new StubReplyGenerator());
        var message = await AddRecommendationAsync(database, service, owner.Id);
        var request = new SaveAdvisorFeedbackDto
        {
            Provider = "eBay",
            ProviderItemId = "item-1",
            Kind = AdvisorFeedbackKind.Helpful,
            Notes = "Good fit"
        };

        var denied = await service.SaveFeedbackAsync(message.Id, other.Id, request);
        var created = await service.SaveFeedbackAsync(message.Id, owner.Id, request);
        request.Kind = AdvisorFeedbackKind.NotInterested;
        var updated = await service.SaveFeedbackAsync(message.Id, owner.Id, request);
        var state = await service.GetStateAsync(message.SessionId, owner.Id);
        var deniedRemoval = await service.RemoveFeedbackAsync(created!.Id, other.Id);

        Assert.Null(denied);
        Assert.Equal(created.Id, updated!.Id);
        Assert.Equal(AdvisorFeedbackKind.NotInterested, updated.Kind);
        Assert.Single(await database.Context.AdvisorRecommendationFeedback.ToListAsync());
        Assert.False(deniedRemoval);
        Assert.Equal(
            AdvisorFeedbackKind.NotInterested,
            state!.Session.Messages.Single().RecommendationCards.Single().Feedback!.Kind);
        Assert.True(await service.RemoveFeedbackAsync(created.Id, owner.Id));
        Assert.Empty(await database.Context.AdvisorRecommendationFeedback.ToListAsync());
    }

    [Fact]
    public async Task Wishlist_action_uses_persisted_card_and_reports_duplicate()
    {
        await using var database = await TestDatabase.CreateAsync();
        var owner = TestDatabase.User("owner");
        var other = TestDatabase.User("other");
        database.Context.Users.AddRange(owner, other);
        await database.Context.SaveChangesAsync();
        var service = CreateService(database, new StubReplyGenerator());
        var message = await AddRecommendationAsync(database, service, owner.Id);
        var request = new AdvisorRecommendationActionDto
        {
            Provider = "eBay",
            ProviderItemId = "item-1"
        };

        var denied = await service.AddToWishlistAsync(message.Id, other.Id, request);
        var created = await service.AddToWishlistAsync(message.Id, owner.Id, request);
        var duplicate = await service.AddToWishlistAsync(message.Id, owner.Id, request);
        var watch = Assert.Single(await database.Context.Watches.ToListAsync());

        Assert.Null(denied);
        Assert.True(created!.Added);
        Assert.False(duplicate!.Added);
        Assert.Equal(created.WatchId, duplicate.WatchId);
        Assert.Equal("Hamilton", watch.Brand);
        Assert.Equal("Khaki Field", watch.Model);
        Assert.Equal(MovementType.Unknown, watch.MovementType);
        Assert.Equal("item-1", watch.MarketplaceItemId);
        Assert.Equal(995m, watch.PurchasePrice);
        Assert.Equal("USD", watch.MarketplaceCurrency);
        Assert.NotNull(watch.MarketplaceObservedAt);
    }

    private static async Task<AdvisorMessage> AddRecommendationAsync(
        TestDatabase database,
        CollectionAdvisorService service,
        int userId)
    {
        var state = await service.StartNewSessionAsync(userId);
        var card = new AdvisorRecommendationCardDto
        {
            Provider = "eBay",
            ProviderItemId = "item-1",
            Title = "Hamilton Khaki Field",
            Brand = "Hamilton",
            Model = "Khaki Field",
            ReferenceNumber = "H70455533",
            ItemUrl = "https://example.test/item-1",
            Price = 995m,
            Currency = "USD",
            ObservedAt = DateTime.UtcNow
        };
        var message = new AdvisorMessage
        {
            SessionId = state.Session.Id,
            Role = AdvisorMessageRole.Assistant,
            Content = "Recommendation",
            RecommendationCardsJson = JsonSerializer.Serialize(
                new[] { card },
                new JsonSerializerOptions(JsonSerializerDefaults.Web))
        };
        database.Context.AdvisorMessages.Add(message);
        await database.Context.SaveChangesAsync();
        return message;
    }

    private static CollectionAdvisorService CreateService(
        TestDatabase database,
        IAdvisorReplyGenerator generator) =>
        new(
            database.Context,
            new CollectionProfileService(database.Context),
            generator);

    private sealed class StubReplyGenerator : IAdvisorReplyGenerator
    {
        public Exception? Failure { get; set; }
        public AdvisorGeneratedReply Reply { get; set; } =
            new("Answer", [], [], [], []);

        public Task<bool> IsConfiguredAsync() => Task.FromResult(true);

        public Task<AdvisorGeneratedReply> GenerateAsync(
            int userId,
            CollectionProfileDto profile,
            IReadOnlyList<AdvisorMessage> history,
            string userMessage,
            CancellationToken ct = default)
        {
            if (Failure is not null) return Task.FromException<AdvisorGeneratedReply>(Failure);
            return Task.FromResult(Reply);
        }
    }
}
