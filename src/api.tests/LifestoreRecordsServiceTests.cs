using WatchTracker.Api.DTOs;
using WatchTracker.Api.Models;
using WatchTracker.Api.Services;

namespace WatchTracker.Api.Tests;

public class LifestoreRecordsServiceTests
{
    private static async Task<(TestDatabase Db, User Owner, Watch Worn, Watch Sold, Watch Wish)> SeedAsync()
    {
        var db = await TestDatabase.CreateAsync();
        var owner = TestDatabase.User("owner");
        var other = TestDatabase.User("other");
        db.Context.Users.AddRange(owner, other);
        await db.Context.SaveChangesAsync();

        var created = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var worn = new Watch
        {
            UserId = owner.Id, Brand = "Seiko", Model = "SKX007", MovementType = MovementType.Automatic,
            PurchaseDate = new DateTime(2024, 5, 4), PurchasePrice = 280m, AcquiredFrom = "Long Island Watch",
            Notes = "Pepsi bezel swapped for black", CreatedAt = created, UpdatedAt = created,
        };
        var sold = new Watch
        {
            UserId = owner.Id, Brand = "Tudor", Model = "Black Bay 58", PurchasePrice = 3400m,
            PurchaseDate = new DateTime(2022, 3, 1), CreatedAt = created, UpdatedAt = created,
        };
        var wish = new Watch { UserId = owner.Id, Brand = "Omega", Model = "Speedmaster", IsWishList = true, WishlistPriority = 1, CreatedAt = created, UpdatedAt = created };
        var someoneElses = new Watch { UserId = other.Id, Brand = "Casio", Model = "F-91W" };
        db.Context.Watches.AddRange(worn, sold, wish, someoneElses);
        await db.Context.SaveChangesAsync();

        db.Context.Labels.Add(new Label { UserId = owner.Id, Name = "Diver", Watches = [worn] });
        db.Context.WearLogs.AddRange(
            new WearLog { WatchId = worn.Id, UserId = owner.Id, WornDate = new DateTime(2026, 10, 2), CreatedAt = new DateTime(2026, 10, 2, 8, 0, 0, DateTimeKind.Utc) },
            new WearLog { WatchId = worn.Id, UserId = owner.Id, WornDate = new DateTime(2026, 10, 1), CreatedAt = new DateTime(2026, 10, 1, 8, 0, 0, DateTimeKind.Utc) });
        db.Context.ResaleValueEntries.Add(new ResaleValueEntry
        {
            WatchId = worn.Id, UserId = owner.Id, Value = 320m, Source = ResaleValueSource.WebSearchEstimate,
            RecordedAt = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc), CreatedAt = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
        });
        db.Context.WatchDispositions.Add(new WatchDisposition
        {
            WatchId = sold.Id, Type = DispositionType.Sold, DispositionDate = new DateTime(2026, 3, 2), SoldTo = "Forum buyer", SalePrice = 3100m,
        });
        await db.Context.SaveChangesAsync();
        db.Context.ChangeTracker.Clear();
        return (db, owner, worn, sold, wish);
    }

    [Fact]
    public async Task Lists_every_watch_the_owner_has_with_its_children()
    {
        var (db, owner, worn, sold, wish) = await SeedAsync();
        await using var _db = db;

        var page = await new LifestoreRecordsService(db.Context).PageAsync(owner.Id, null, 0);

        Assert.Equal(LifestoreRecordsPageDto.SchemaName, page.Schema);
        Assert.Null(page.Next);
        Assert.Equal([worn.Id.ToString(), sold.Id.ToString(), wish.Id.ToString()], page.Records.Select(r => r.Id));
        Assert.All(page.Records, r => Assert.Equal("watch", r.Kind));

        var w = page.Records[0];
        Assert.Equal("Long Island Watch", w.Data.AcquiredFrom);
        Assert.Equal(["Diver"], w.Data.Labels);
        Assert.Equal([new DateTime(2026, 10, 1), new DateTime(2026, 10, 2)], w.Data.Wears.Select(l => l.WornDate));
        Assert.Equal(320m, Assert.Single(w.Data.ResaleValues).Value);
        // The newest wear is the latest change.
        Assert.Equal(new DateTime(2026, 10, 2, 8, 0, 0), w.UpdatedAt);

        var s = page.Records[1].Data.Disposition;
        Assert.NotNull(s);
        Assert.Equal(DispositionType.Sold, s.Type);
        Assert.Equal(3100m, s.SalePrice);
        Assert.True(page.Records[2].Data.IsWishList);
    }

    [Fact]
    public async Task Pages_follow_the_token_and_stop()
    {
        var (db, owner, worn, sold, wish) = await SeedAsync();
        await using var _db = db;
        var service = new LifestoreRecordsService(db.Context);

        var first = await service.PageAsync(owner.Id, null, 2);
        Assert.Equal([worn.Id.ToString(), sold.Id.ToString()], first.Records.Select(r => r.Id));
        Assert.Equal($"watch:{sold.Id}", first.Next);

        var second = await service.PageAsync(owner.Id, first.Next, 2);
        Assert.Equal([wish.Id.ToString()], second.Records.Select(r => r.Id));
        Assert.Null(second.Next);
    }

    [Theory]
    [InlineData("coin:1")]
    [InlineData("watch:")]
    [InlineData("watch:-1")]
    [InlineData("watch:1x")]
    public async Task A_token_it_did_not_give_out_is_refused(string token)
    {
        var (db, owner, _, _, _) = await SeedAsync();
        await using var _db = db;

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            new LifestoreRecordsService(db.Context).PageAsync(owner.Id, token, 10));
        Assert.Contains("Start again without a page", ex.Message);
    }

    [Fact]
    public async Task Limit_is_clamped()
    {
        var (db, owner, _, _, _) = await SeedAsync();
        await using var _db = db;

        var page = await new LifestoreRecordsService(db.Context).PageAsync(owner.Id, null, 10_000);
        Assert.Equal(3, page.Records.Count);
    }
}
