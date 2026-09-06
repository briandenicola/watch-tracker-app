using WatchTracker.Api.Models;
using WatchTracker.Api.Services;

namespace WatchTracker.Api.Tests;

public class CollectionProfileServiceTests
{
    [Fact]
    public async Task Profile_excludes_other_users_wishlist_and_disposed_watches()
    {
        await using var database = await TestDatabase.CreateAsync();
        var owner = TestDatabase.User("owner");
        var other = TestDatabase.User("other");
        database.Context.Users.AddRange(owner, other);
        await database.Context.SaveChangesAsync();

        var active = Watch(owner.Id, "Active", "One");
        var wishlist = Watch(owner.Id, "Wish", "One");
        wishlist.IsWishList = true;
        wishlist.WishlistPriority = 0;
        var disposed = Watch(owner.Id, "Former", "One");
        disposed.Disposition = new WatchDisposition
        {
            Type = DispositionType.Sold,
            DispositionDate = DateTime.UtcNow
        };
        database.Context.Watches.AddRange(
            active,
            wishlist,
            disposed,
            Watch(other.Id, "Other", "One"));
        await database.Context.SaveChangesAsync();

        var profile = await new CollectionProfileService(database.Context)
            .GetProfileAsync(owner.Id);

        Assert.Equal(1, profile.ActiveWatchCount);
        Assert.Equal(1, profile.WishlistWatchCount);
        Assert.All(
            profile.Coverage.SelectMany(c => c.Values).SelectMany(v => v.WatchIds),
            watchId => Assert.Equal(active.Id, watchId));
    }

    [Fact]
    public async Task Sparse_collection_reports_missing_data_without_false_variety_gaps()
    {
        await using var database = await TestDatabase.CreateAsync();
        var owner = TestDatabase.User("owner");
        database.Context.Users.Add(owner);
        await database.Context.SaveChangesAsync();
        database.Context.Watches.Add(Watch(owner.Id, "Sparse", "One"));
        await database.Context.SaveChangesAsync();

        var profile = await new CollectionProfileService(database.Context)
            .GetProfileAsync(owner.Id);

        Assert.Empty(profile.Gaps);
        Assert.True(profile.DataCompletenessPercent < 50);
        Assert.Contains(profile.DataQuality, insight => insight.Summary == "Missing case size data");
    }

    [Fact]
    public async Task Profile_reports_the_strongest_watch_to_watch_attribute_overlap()
    {
        await using var database = await TestDatabase.CreateAsync();
        var owner = TestDatabase.User("owner");
        database.Context.Users.Add(owner);
        await database.Context.SaveChangesAsync();
        var first = Watch(owner.Id, "Omega", "Seamaster");
        first.CaseSizeMm = 40;
        first.DialColor = "Blue";
        first.BandType = "Bracelet";
        var second = Watch(owner.Id, "Tissot", "PRX");
        second.CaseSizeMm = 39;
        second.DialColor = "Blue";
        second.BandType = "Bracelet";
        var different = Watch(owner.Id, "Casio", "G-Shock");
        different.MovementType = MovementType.Quartz;
        different.CaseSizeMm = 45;
        different.DialColor = "Black";
        different.BandType = "Resin";
        database.Context.Watches.AddRange(first, second, different);
        await database.Context.SaveChangesAsync();

        var profile = await new CollectionProfileService(database.Context)
            .GetProfileAsync(owner.Id);

        var overlap = Assert.Single(
            profile.Redundancies,
            insight => insight.Summary == "Strong watch-to-watch overlap");
        Assert.Equal([first.Id, second.Id], overlap.WatchIds);
        Assert.Contains("Omega Seamaster", overlap.Reason);
        Assert.Contains("Tissot PRX", overlap.Reason);
        Assert.Equal(
            ["Movement", "Case size", "Dial color", "Band type"],
            overlap.EvidenceFields);
    }

    [Fact]
    public async Task Pair_overlap_does_not_repeat_a_larger_trait_cluster()
    {
        await using var database = await TestDatabase.CreateAsync();
        var owner = TestDatabase.User("owner");
        database.Context.Users.Add(owner);
        await database.Context.SaveChangesAsync();
        for (var index = 1; index <= 3; index++)
        {
            var watch = Watch(owner.Id, $"Brand {index}", $"Model {index}");
            watch.CaseSizeMm = 40;
            watch.DialColor = "Blue";
            watch.BandType = index == 1 ? "Leather" : "Bracelet";
            database.Context.Watches.Add(watch);
        }
        await database.Context.SaveChangesAsync();

        var profile = await new CollectionProfileService(database.Context)
            .GetProfileAsync(owner.Id);

        Assert.Single(
            profile.Redundancies,
            insight => insight.Summary == "Repeated movement, size, and dial profile");
        Assert.DoesNotContain(
            profile.Redundancies,
            insight => insight.Summary == "Strong watch-to-watch overlap");
    }

    private static Watch Watch(int userId, string brand, string model) => new()
    {
        UserId = userId,
        Brand = brand,
        Model = model,
        MovementType = MovementType.Automatic,
        CreatedAt = DateTime.UtcNow.AddDays(-60)
    };
}
