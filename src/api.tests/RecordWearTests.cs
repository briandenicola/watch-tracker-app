using WatchTracker.Api.DTOs;
using WatchTracker.Api.Models;
using WatchTracker.Api.Services;

namespace WatchTracker.Api.Tests;

public class RecordWearTests
{
    [Fact]
    public async Task Wear_without_a_body_is_stamped_now()
    {
        await using var database = await TestDatabase.CreateAsync();
        var (user, watch) = await AddWatchAsync(database);
        var service = new WatchWearLogService(database.Context);
        var before = DateTime.UtcNow;

        var result = await service.RecordWearAsync(watch.Id, user.Id);

        Assert.NotNull(result);
        Assert.Equal(1, result!.TimesWorn);
        var log = Assert.Single(database.Context.WearLogs);
        Assert.InRange(log.WornDate, before.AddSeconds(-5), DateTime.UtcNow.AddSeconds(5));
        Assert.Equal(log.WornDate, log.StartedAt);
        Assert.Null(log.EndedAt);
    }

    [Fact]
    public async Task Back_dated_wear_is_logged_on_the_requested_day()
    {
        await using var database = await TestDatabase.CreateAsync();
        var (user, watch) = await AddWatchAsync(database);
        var service = new WatchWearLogService(database.Context);
        var worn = new DateTimeOffset(2026, 3, 14, 17, 0, 0, TimeSpan.Zero);

        var result = await service.RecordWearAsync(
            watch.Id,
            user.Id,
            new RecordWearDto { WornDate = worn });

        Assert.NotNull(result);
        var log = Assert.Single(database.Context.WearLogs);
        Assert.Equal(worn.UtcDateTime, log.WornDate);
        Assert.Equal(
            worn.UtcDateTime,
            database.Context.Watches.Single(w => w.Id == watch.Id).LastWornDate);
    }

    [Fact]
    public async Task Back_dated_wear_does_not_drag_last_worn_backwards()
    {
        await using var database = await TestDatabase.CreateAsync();
        var (user, watch) = await AddWatchAsync(database);
        var service = new WatchWearLogService(database.Context);

        // Worn today, then a forgotten wear from last spring is added after.
        await service.RecordWearAsync(watch.Id, user.Id);
        var recent = database.Context.Watches.Single(w => w.Id == watch.Id).LastWornDate;

        await service.RecordWearAsync(
            watch.Id,
            user.Id,
            new RecordWearDto { WornDate = new DateTimeOffset(2026, 3, 14, 17, 0, 0, TimeSpan.Zero) });

        var reloaded = database.Context.Watches.Single(w => w.Id == watch.Id);
        Assert.Equal(recent, reloaded.LastWornDate);
        Assert.Equal(2, reloaded.TimesWorn);
        Assert.Equal(2, database.Context.WearLogs.Count());
    }

    [Fact]
    public async Task Forward_dated_wear_moves_last_worn_forward()
    {
        await using var database = await TestDatabase.CreateAsync();
        var (user, watch) = await AddWatchAsync(database);
        var service = new WatchWearLogService(database.Context);
        var older = new DateTimeOffset(2026, 1, 2, 9, 0, 0, TimeSpan.Zero);
        var newer = new DateTimeOffset(2026, 5, 6, 9, 0, 0, TimeSpan.Zero);

        await service.RecordWearAsync(watch.Id, user.Id, new RecordWearDto { WornDate = older });
        await service.RecordWearAsync(watch.Id, user.Id, new RecordWearDto { WornDate = newer });

        var reloaded = database.Context.Watches.Single(w => w.Id == watch.Id);
        Assert.Equal(newer.UtcDateTime, reloaded.LastWornDate);
    }

    [Fact]
    public async Task Start_and_end_times_are_stored_when_supplied()
    {
        await using var database = await TestDatabase.CreateAsync();
        var (user, watch) = await AddWatchAsync(database);
        var service = new WatchWearLogService(database.Context);
        var started = new DateTimeOffset(2026, 3, 14, 13, 30, 0, TimeSpan.Zero);
        var ended = new DateTimeOffset(2026, 3, 14, 21, 15, 0, TimeSpan.Zero);

        await service.RecordWearAsync(watch.Id, user.Id, new RecordWearDto
        {
            WornDate = started,
            StartedAt = started,
            EndedAt = ended,
        });

        var log = Assert.Single(database.Context.WearLogs);
        Assert.Equal(started.UtcDateTime, log.StartedAt);
        Assert.Equal(ended.UtcDateTime, log.EndedAt);
    }

    [Fact]
    public async Task Wish_list_watches_cannot_be_worn()
    {
        await using var database = await TestDatabase.CreateAsync();
        var (user, watch) = await AddWatchAsync(database, isWishList: true);
        var service = new WatchWearLogService(database.Context);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RecordWearAsync(watch.Id, user.Id));

        Assert.Contains("wish list", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(database.Context.WearLogs);
    }

    [Fact]
    public async Task Another_users_watch_is_not_found()
    {
        await using var database = await TestDatabase.CreateAsync();
        var (_, watch) = await AddWatchAsync(database);
        var stranger = TestDatabase.User("stranger");
        database.Context.Add(stranger);
        await database.Context.SaveChangesAsync();
        var service = new WatchWearLogService(database.Context);

        var result = await service.RecordWearAsync(watch.Id, stranger.Id);

        Assert.Null(result);
        Assert.Empty(database.Context.WearLogs);
    }

    private static async Task<(User User, Watch Watch)> AddWatchAsync(
        TestDatabase database,
        bool isWishList = false)
    {
        var user = TestDatabase.User("owner");
        var watch = new Watch
        {
            Brand = "Seiko",
            Model = "SPB143",
            IsWishList = isWishList,
            User = user
        };
        database.Context.AddRange(user, watch);
        await database.Context.SaveChangesAsync();
        return (user, watch);
    }
}
