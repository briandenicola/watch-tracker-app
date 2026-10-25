using WatchTracker.Api.DTOs;
using WatchTracker.Api.Models;
using WatchTracker.Api.Services;

namespace WatchTracker.Api.Tests;

public class SavedViewServiceTests
{
    private static async Task<(TestDatabase Db, User Owner, User Other)> SeedAsync()
    {
        var db = await TestDatabase.CreateAsync();
        var owner = TestDatabase.User("owner");
        var other = TestDatabase.User("other");
        db.Context.Users.AddRange(owner, other);
        await db.Context.SaveChangesAsync();
        return (db, owner, other);
    }

    private static SaveSavedViewDto View(string name, bool pinned = false) => new() { Name = name, Pinned = pinned };

    [Fact]
    public async Task Saves_every_filter_and_returns_it_unchanged()
    {
        var (db, owner, _) = await SeedAsync();
        await using var _db = db;
        var label = await new LabelService(db.Context).CreateAsync(owner.Id, new SaveLabelDto { Name = "Diver" });

        var created = await new SavedViewService(db.Context).CreateAsync(owner.Id, new SaveSavedViewDto
        {
            Name = " Wishlist divers ",
            Scope = SavedViewScope.Wishlist,
            LabelIds = [label.Id],
            LabelMatch = LabelMatchMode.All,
            Brand = "Omega",
            MovementType = MovementType.Manual,
            SortBy = "priority",
            Pinned = true,
        });

        db.Context.ChangeTracker.Clear();
        var view = Assert.Single(await new SavedViewService(db.Context).GetAllAsync(owner.Id));
        Assert.Equal(created.Id, view.Id);
        Assert.Equal("Wishlist divers", view.Name);
        Assert.Equal(SavedViewScope.Wishlist, view.Scope);
        Assert.Equal([label.Id], view.LabelIds);
        Assert.Equal(LabelMatchMode.All, view.LabelMatch);
        Assert.Equal("Omega", view.Brand);
        Assert.Equal(MovementType.Manual, view.MovementType);
        Assert.Equal("priority", view.SortBy);
        Assert.True(view.Pinned);
    }

    [Fact]
    public async Task Rejects_priority_sort_for_the_collection_and_unknown_sorts()
    {
        var (db, owner, _) = await SeedAsync();
        await using var _db = db;
        var service = new SavedViewService(db.Context);

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(owner.Id,
            new SaveSavedViewDto { Name = "A", Scope = SavedViewScope.Collection, SortBy = "priority" }));
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(owner.Id,
            new SaveSavedViewDto { Name = "B", SortBy = "price" }));
    }

    [Fact]
    public async Task Allows_five_pinned_views_and_rejects_a_sixth()
    {
        var (db, owner, other) = await SeedAsync();
        await using var _db = db;
        var service = new SavedViewService(db.Context);
        for (var i = 1; i <= SavedViewService.PinLimit; i++)
            await service.CreateAsync(owner.Id, View($"Pinned {i}", pinned: true));
        var extra = await service.CreateAsync(owner.Id, View("Extra"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(owner.Id, View("Too many", pinned: true)));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SetPinnedAsync(extra.Id, owner.Id, true));
        // Another user's pins do not count against this user's limit.
        await service.CreateAsync(other.Id, View("Theirs", pinned: true));

        var first = (await service.GetAllAsync(owner.Id)).First();
        await service.SetPinnedAsync(first.Id, owner.Id, false);
        Assert.True((await service.SetPinnedAsync(extra.Id, owner.Id, true))!.Pinned);
    }

    [Fact]
    public async Task Lists_pinned_views_in_pin_order_then_the_rest_by_name()
    {
        var (db, owner, _) = await SeedAsync();
        await using var _db = db;
        var service = new SavedViewService(db.Context);
        await service.CreateAsync(owner.Id, View("Zulu"));
        var b = await service.CreateAsync(owner.Id, View("Bravo"));
        await service.CreateAsync(owner.Id, View("alpha"));
        var y = await service.CreateAsync(owner.Id, View("Yankee"));
        await service.SetPinnedAsync(y.Id, owner.Id, true);
        await service.SetPinnedAsync(b.Id, owner.Id, true);

        Assert.Equal(["Yankee", "Bravo", "alpha", "Zulu"], (await service.GetAllAsync(owner.Id)).Select(v => v.Name));
    }

    [Fact]
    public async Task Keeps_views_private_to_their_owner()
    {
        var (db, owner, other) = await SeedAsync();
        await using var _db = db;
        var service = new SavedViewService(db.Context);
        var mine = await service.CreateAsync(owner.Id, View("Mine"));
        var theirLabel = await new LabelService(db.Context).CreateAsync(other.Id, new SaveLabelDto { Name = "Theirs" });

        Assert.Empty(await service.GetAllAsync(other.Id));
        Assert.Null(await service.UpdateAsync(mine.Id, other.Id, View("Taken")));
        Assert.Null(await service.SetPinnedAsync(mine.Id, other.Id, true));
        Assert.False(await service.DeleteAsync(mine.Id, other.Id));
        await Assert.ThrowsAsync<ArgumentException>(() => service.UpdateAsync(mine.Id, owner.Id,
            new SaveSavedViewDto { Name = "Mine", LabelIds = [theirLabel.Id] }));
        // The same name is fine for a different user, a duplicate for the same user is not.
        await service.CreateAsync(other.Id, View("Mine"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(owner.Id, View("MINE")));
    }
}
