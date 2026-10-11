using System.Text.Json;
using WatchTracker.Api.DTOs;
using WatchTracker.Api.Models;
using WatchTracker.Api.Services;

namespace WatchTracker.Api.Tests;

public class StorageSlotServiceTests
{
    private static async Task<(TestDatabase Db, User Owner, Watch A, Watch B)> SeedAsync()
    {
        var db = await TestDatabase.CreateAsync();
        var owner = TestDatabase.User("owner");
        owner.StorageLayoutsJson = JsonSerializer.Serialize(
            new Dictionary<string, StorageLayoutDto> { ["Box"] = new() { Rows = 1, Columns = 2 } });
        db.Context.Users.Add(owner);
        await db.Context.SaveChangesAsync();

        var a = new Watch { UserId = owner.Id, Brand = "A", Model = "One", StorageLocation = "Box" };
        var b = new Watch { UserId = owner.Id, Brand = "B", Model = "Two", StorageLocation = "Box" };
        db.Context.Watches.AddRange(a, b);
        await db.Context.SaveChangesAsync();
        return (db, owner, a, b);
    }

    [Fact]
    public async Task Saves_slots_for_watches_in_the_location()
    {
        var (db, owner, a, b) = await SeedAsync();
        await using var _ = db;

        await new StorageSlotService(db.Context).UpdateSlotsAsync(owner.Id, new UpdateStorageSlotsDto
        {
            Location = "Box",
            Placements = [new() { WatchId = a.Id, Slot = 1 }, new() { WatchId = b.Id, Slot = 0 }]
        });

        Assert.Equal(1, a.StorageSlot);
        Assert.Equal(0, b.StorageSlot);
    }

    [Fact]
    public async Task Rejects_slots_outside_the_grid_and_duplicate_slots()
    {
        var (db, owner, a, b) = await SeedAsync();
        await using var _ = db;
        var service = new StorageSlotService(db.Context);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateSlotsAsync(owner.Id,
            new UpdateStorageSlotsDto { Location = "Box", Placements = [new() { WatchId = a.Id, Slot = 2 }] }));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateSlotsAsync(owner.Id,
            new UpdateStorageSlotsDto { Location = "Box", Placements = [new() { WatchId = a.Id, Slot = 0 }, new() { WatchId = b.Id, Slot = 0 }] }));
    }

    [Fact]
    public async Task Rejects_watches_from_another_location_or_user()
    {
        var (db, owner, a, _) = await SeedAsync();
        await using var _db = db;
        a.StorageLocation = "Safe";
        await db.Context.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => new StorageSlotService(db.Context).UpdateSlotsAsync(owner.Id,
            new UpdateStorageSlotsDto { Location = "Box", Placements = [new() { WatchId = a.Id, Slot = 0 }] }));
        await Assert.ThrowsAsync<InvalidOperationException>(() => new StorageSlotService(db.Context).UpdateSlotsAsync(owner.Id + 99,
            new UpdateStorageSlotsDto { Location = "Box", Placements = [new() { WatchId = a.Id, Slot = 0 }] }));
    }
}
