using Microsoft.EntityFrameworkCore;
using WatchTracker.Api.Data;
using WatchTracker.Api.DTOs;
using WatchTracker.Api.Models;

namespace WatchTracker.Api.Services;

public class ResaleValueService(AppDbContext context) : IResaleValueService
{
    public async Task<WatchDto?> AddManualAsync(int watchId, int userId, CreateResaleValueEntryDto dto, CancellationToken ct = default)
    {
        var watch = await context.Watches
            .Include(w => w.Images)
            .FirstOrDefaultAsync(w => w.Id == watchId && w.UserId == userId, ct);
        if (watch is null) return null;

        var recordedAt = dto.RecordedAt ?? DateTime.UtcNow;
        context.ResaleValueEntries.Add(new ResaleValueEntry
        {
            WatchId = watch.Id,
            UserId = userId,
            Value = dto.Value,
            Source = ResaleValueSource.Manual,
            Reasoning = dto.Notes,
            RecordedAt = recordedAt,
        });

        if (watch.ResaleValueUpdatedAt is null || recordedAt >= watch.ResaleValueUpdatedAt)
        {
            watch.CurrentResaleValue = dto.Value;
            watch.ResaleValueUpdatedAt = recordedAt;
        }
        watch.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);
        return WatchDtoMapper.Map(watch);
    }

    public async Task<IEnumerable<ResaleValueEntryDto>> GetHistoryAsync(int watchId, int userId, CancellationToken ct = default) =>
        await context.ResaleValueEntries
            .Where(r => r.WatchId == watchId && r.UserId == userId)
            .OrderByDescending(r => r.RecordedAt)
            .Select(r => new ResaleValueEntryDto
            {
                Id = r.Id,
                WatchId = r.WatchId,
                Value = r.Value,
                Source = r.Source,
                Reasoning = r.Reasoning,
                RecordedAt = r.RecordedAt,
            })
            .ToListAsync(ct);

    public async Task<bool> DeleteEntryAsync(int entryId, int userId, CancellationToken ct = default)
    {
        var entry = await context.ResaleValueEntries
            .Include(r => r.Watch)
            .FirstOrDefaultAsync(r => r.Id == entryId && r.UserId == userId, ct);
        if (entry is null) return false;

        context.ResaleValueEntries.Remove(entry);
        var nextMostRecent = await context.ResaleValueEntries
            .Where(r => r.WatchId == entry.WatchId && r.Id != entryId)
            .OrderByDescending(r => r.RecordedAt)
            .FirstOrDefaultAsync(ct);

        entry.Watch.CurrentResaleValue = nextMostRecent?.Value;
        entry.Watch.ResaleValueUpdatedAt = nextMostRecent?.RecordedAt;
        entry.Watch.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(ct);
        return true;
    }
}
