using Microsoft.EntityFrameworkCore;
using WatchTracker.Api.Data;
using WatchTracker.Api.DTOs;
using WatchTracker.Api.Models;

namespace WatchTracker.Api.Services;

public class WatchWearLogService(AppDbContext context) : IWatchWearLogService
{
    public async Task<WatchDto?> RecordWearAsync(int id, int userId, RecordWearDto? dto = null, CancellationToken ct = default)
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var watch = await context.Watches
                .Include(w => w.Images)
                .Include(w => w.Disposition)
                    .ThenInclude(d => d!.ReceivedWatch)
                .FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId, ct);

            if (watch is null) return null;
            if (watch.Disposition is not null)
                throw new InvalidOperationException("Wear cannot be recorded for a former watch.");
            if (watch.IsWishList)
                throw new InvalidOperationException("Wear cannot be recorded for a wish list watch.");

            var now = DateTime.UtcNow;
            var wornAt = dto?.WornDate?.UtcDateTime ?? now;
            watch.TimesWorn++;
            watch.LastWornDate = watch.LastWornDate is { } previous && previous > wornAt ? previous : wornAt;
            watch.UpdatedAt = now;

            context.WearLogs.Add(new WearLog
            {
                WatchId = watch.Id,
                UserId = userId,
                WornDate = wornAt,
                StartedAt = dto?.StartedAt?.UtcDateTime ?? wornAt,
                EndedAt = dto?.EndedAt?.UtcDateTime,
            });

            try
            {
                await context.SaveChangesAsync(ct);
                return WatchDtoMapper.Map(watch);
            }
            catch (DbUpdateConcurrencyException) when (attempt < 2)
            {
                foreach (var entry in context.ChangeTracker.Entries())
                    await entry.ReloadAsync(ct);
            }
        }

        return null;
    }

    public async Task<IEnumerable<WearLogDto>> GetWearLogsAsync(int userId, CancellationToken ct = default) =>
        await context.WearLogs
            .Include(wl => wl.Watch)
            .ThenInclude(w => w.Images)
            .Where(wl => wl.UserId == userId)
            .OrderByDescending(wl => wl.WornDate)
            .Select(wl => MapWearLogDto(wl))
            .ToListAsync(ct);

    public async Task<bool> DeleteWearLogAsync(int logId, int userId, CancellationToken ct = default)
    {
        var log = await context.WearLogs
            .Include(wl => wl.Watch)
            .FirstOrDefaultAsync(wl => wl.Id == logId && wl.UserId == userId, ct);
        if (log is null) return false;

        log.Watch.TimesWorn = Math.Max(0, log.Watch.TimesWorn - 1);
        log.Watch.LastWornDate = (await context.WearLogs
            .Where(wl => wl.WatchId == log.WatchId && wl.Id != logId)
            .OrderByDescending(wl => wl.WornDate)
            .FirstOrDefaultAsync(ct))?.WornDate;

        context.WearLogs.Remove(log);
        await context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> UpdateWearLogAsync(int logId, int userId, UpdateWearLogDateDto dto, CancellationToken ct = default)
    {
        var log = await context.WearLogs
            .Include(wl => wl.Watch)
            .ThenInclude(w => w.WearLogs)
            .FirstOrDefaultAsync(wl => wl.Id == logId && wl.UserId == userId, ct);
        if (log is null) return false;

        log.WornDate = dto.WornDate.UtcDateTime;
        log.StartedAt = dto.StartedAt?.UtcDateTime;
        log.EndedAt = dto.EndedAt?.UtcDateTime;
        log.Watch.LastWornDate = log.Watch.WearLogs
            .Select(wl => wl.Id == logId ? dto.WornDate.UtcDateTime : wl.WornDate)
            .OrderByDescending(date => date)
            .FirstOrDefault();

        await context.SaveChangesAsync(ct);
        return true;
    }

    private static WearLogDto MapWearLogDto(WearLog log)
    {
        var duration = log.StartedAt is not null && log.EndedAt is not null && log.EndedAt > log.StartedAt
            ? (int)Math.Round((log.EndedAt.Value - log.StartedAt.Value).TotalMinutes)
            : (int?)null;
        return new WearLogDto
        {
            Id = log.Id,
            WatchId = log.WatchId,
            WatchBrand = log.Watch.Brand,
            WatchModel = log.Watch.Model,
            WornDate = DateTime.SpecifyKind(log.WornDate, DateTimeKind.Utc),
            StartedAt = log.StartedAt is null ? null : DateTime.SpecifyKind(log.StartedAt.Value, DateTimeKind.Utc),
            EndedAt = log.EndedAt is null ? null : DateTime.SpecifyKind(log.EndedAt.Value, DateTimeKind.Utc),
            DurationMinutes = duration,
            WatchImageUrl = log.Watch.Images.OrderBy(i => i.SortOrder).Select(i => $"/uploads/{i.FileName}").FirstOrDefault(),
        };
    }
}
