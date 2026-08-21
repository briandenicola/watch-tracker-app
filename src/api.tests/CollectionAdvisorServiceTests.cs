using Microsoft.EntityFrameworkCore;
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
