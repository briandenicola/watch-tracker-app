using Microsoft.EntityFrameworkCore;
using WatchTracker.Api.Data;
using WatchTracker.Api.DTOs;

namespace WatchTracker.Api.Services;

public class StorageSlotService(AppDbContext context) : IStorageSlotService
{
    public async Task UpdateSlotsAsync(int userId, UpdateStorageSlotsDto dto, CancellationToken ct = default)
    {
        var user = await context.Users.FindAsync([userId], ct)
            ?? throw new InvalidOperationException("User not found.");

        if (!StorageLayouts.Parse(user.StorageLayoutsJson).TryGetValue(dto.Location, out var layout))
            throw new InvalidOperationException($"'{dto.Location}' has no grid layout.");

        var capacity = StorageLayouts.Capacity(layout);
        if (dto.Placements.Any(p => p.Slot >= capacity))
            throw new InvalidOperationException("A slot is outside the storage grid.");
        if (dto.Placements.Select(p => p.Slot).Distinct().Count() != dto.Placements.Count
            || dto.Placements.Select(p => p.WatchId).Distinct().Count() != dto.Placements.Count)
            throw new InvalidOperationException("Placements must use unique watches and slots.");

        var watchIds = dto.Placements.Select(p => p.WatchId).ToList();
        var watches = await context.Watches
            .Where(w => w.UserId == userId && watchIds.Contains(w.Id))
            .ToListAsync(ct);

        if (watches.Count != watchIds.Count || watches.Any(w => w.StorageLocation != dto.Location))
            throw new InvalidOperationException("Every watch must belong to this storage location.");

        var slots = dto.Placements.ToDictionary(p => p.WatchId, p => p.Slot);
        foreach (var watch in watches)
        {
            watch.StorageSlot = slots[watch.Id];
            watch.UpdatedAt = DateTime.UtcNow;
        }

        await context.SaveChangesAsync(ct);
    }
}
