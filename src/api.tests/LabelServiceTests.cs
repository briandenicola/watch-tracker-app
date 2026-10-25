using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using WatchTracker.Api.DTOs;
using WatchTracker.Api.Models;
using WatchTracker.Api.Services;

namespace WatchTracker.Api.Tests;

public class LabelServiceTests
{
    private static async Task<(TestDatabase Db, User Owner, User Other, Watch Watch, Watch Wish)> SeedAsync()
    {
        var db = await TestDatabase.CreateAsync();
        var owner = TestDatabase.User("owner");
        var other = TestDatabase.User("other");
        db.Context.Users.AddRange(owner, other);
        await db.Context.SaveChangesAsync();

        var watch = new Watch { UserId = owner.Id, Brand = "Seiko", Model = "SKX007" };
        var wish = new Watch { UserId = owner.Id, Brand = "Omega", Model = "Speedmaster", IsWishList = true };
        db.Context.Watches.AddRange(watch, wish);
        await db.Context.SaveChangesAsync();
        return (db, owner, other, watch, wish);
    }

    [Fact]
    public async Task Labels_apply_to_watches_and_wishlist_items_and_appear_on_the_watch()
    {
        var (db, owner, _, watch, wish) = await SeedAsync();
        await using var _db = db;
        var service = new LabelService(db.Context);

        var diver = await service.CreateAsync(owner.Id, new SaveLabelDto { Name = "Diver", Color = "#1e90ff" });
        var grail = await service.CreateAsync(owner.Id, new SaveLabelDto { Name = "Grail" });

        Assert.Equal([diver.Id, grail.Id], await service.SetWatchLabelsAsync(watch.Id, owner.Id, [grail.Id, diver.Id]));
        await service.SetWatchLabelsAsync(wish.Id, owner.Id, [grail.Id]);

        db.Context.ChangeTracker.Clear();
        var watches = (await new WatchCatalogService(db.Context, null!, NullLogger<WatchCatalogService>.Instance)
            .GetAllAsync(owner.Id)).ToDictionary(w => w.Id);
        Assert.Equal([diver.Id, grail.Id], watches[watch.Id].LabelIds);
        Assert.Equal([grail.Id], watches[wish.Id].LabelIds);

        var labels = await service.GetAllAsync(owner.Id);
        Assert.Equal(["Diver", "Grail"], labels.Select(l => l.Name));
        Assert.Equal([1, 2], labels.Select(l => l.WatchCount));
        Assert.Equal(LabelService.DefaultColor, labels[1].Color);
    }

    [Fact]
    public async Task Rejects_duplicate_names_ignoring_case_but_allows_the_same_name_for_another_user()
    {
        var (db, owner, other, _, _) = await SeedAsync();
        await using var _db = db;
        var service = new LabelService(db.Context);

        var label = await service.CreateAsync(owner.Id, new SaveLabelDto { Name = "Diver" });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAsync(owner.Id, new SaveLabelDto { Name = " diver " }));
        await service.CreateAsync(other.Id, new SaveLabelDto { Name = "Diver" });

        // Renaming a label to its own name in a different case is not a conflict.
        var renamed = await service.UpdateAsync(label.Id, owner.Id, new SaveLabelDto { Name = "DIVER" });
        Assert.Equal("DIVER", renamed!.Name);
    }

    [Fact]
    public async Task Cannot_use_another_users_labels_or_label_their_watches()
    {
        var (db, owner, other, watch, _) = await SeedAsync();
        await using var _db = db;
        var service = new LabelService(db.Context);
        var theirs = await service.CreateAsync(other.Id, new SaveLabelDto { Name = "Theirs" });
        var mine = await service.CreateAsync(owner.Id, new SaveLabelDto { Name = "Mine" });

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.SetWatchLabelsAsync(watch.Id, owner.Id, [theirs.Id]));
        Assert.Null(await service.SetWatchLabelsAsync(watch.Id, other.Id, [theirs.Id]));
        Assert.Null(await service.UpdateAsync(mine.Id, other.Id, new SaveLabelDto { Name = "Stolen" }));
        Assert.False(await service.DeleteAsync(mine.Id, other.Id));
    }

    [Fact]
    public async Task Deleting_a_label_removes_it_from_watches_and_saved_views()
    {
        var (db, owner, _, watch, _) = await SeedAsync();
        await using var _db = db;
        var labels = new LabelService(db.Context);
        var label = await labels.CreateAsync(owner.Id, new SaveLabelDto { Name = "Diver" });
        await labels.SetWatchLabelsAsync(watch.Id, owner.Id, [label.Id]);
        var view = await new SavedViewService(db.Context).CreateAsync(owner.Id,
            new SaveSavedViewDto { Name = "Divers", LabelIds = [label.Id] });

        Assert.True(await labels.DeleteAsync(label.Id, owner.Id));

        db.Context.ChangeTracker.Clear();
        Assert.Empty(await db.Context.Watches.Where(w => w.Id == watch.Id).SelectMany(w => w.Labels).ToListAsync());
        Assert.Empty((await new SavedViewService(db.Context).GetAllAsync(owner.Id)).Single(v => v.Id == view.Id).LabelIds);
    }
}
