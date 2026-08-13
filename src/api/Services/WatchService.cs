using Microsoft.EntityFrameworkCore;
using WatchTracker.Api.Data;
using WatchTracker.Api.DTOs;
using WatchTracker.Api.Models;

namespace WatchTracker.Api.Services;

public class WatchService(AppDbContext context) : IWatchService
{
    public async Task<IEnumerable<WatchDto>> GetAllAsync(int userId, bool includeRetired = false, CancellationToken ct = default)
    {
        var query = context.Watches
            .Include(w => w.Images)
            .Where(w => w.UserId == userId);

        if (!includeRetired)
            query = query.Where(w => !w.IsRetired);

        return await query
            .Select(w => MapToDto(w))
            .ToListAsync(ct);
    }

    public async Task<WatchDto?> GetByIdAsync(int id, int userId, CancellationToken ct = default)
    {
        var watch = await context.Watches
            .Include(w => w.Images)
            .FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId, ct);

        return watch is null ? null : MapToDto(watch);
    }

    public async Task<WatchDto> CreateAsync(CreateWatchDto dto, int userId, CancellationToken ct = default)
    {
        var watch = new Watch
        {
            Brand = dto.Brand,
            Model = dto.Model,
            MovementType = dto.MovementType,
            CaseSizeMm = dto.CaseSizeMm,
            BandType = dto.BandType,
            BandColor = dto.BandColor,
            PurchaseDate = dto.PurchaseDate,
            PurchasePrice = dto.PurchasePrice,
            Notes = dto.Notes,
            CrystalType = dto.CrystalType,
            CaseShape = dto.CaseShape,
            CrownType = dto.CrownType,
            CalendarType = dto.CalendarType,
            CountryOfOrigin = dto.CountryOfOrigin,
            WaterResistance = dto.WaterResistance,
            LugWidthMm = dto.LugWidthMm,
            DialColor = dto.DialColor,
            BezelType = dto.BezelType,
            PowerReserveHours = dto.PowerReserveHours,
            Sku = dto.Sku,
            SerialNumber = dto.SerialNumber,
            ProductionYear = dto.ProductionYear,
            BatteryType = dto.BatteryType,
            LastBatteryChangedDate = dto.LastBatteryChangedDate,
            LinkUrl = dto.LinkUrl,
            LinkText = dto.LinkText,
            StorageLocation = dto.StorageLocation,
            IsWishList = dto.IsWishList,
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Watches.Add(watch);
        await context.SaveChangesAsync(ct);

        return MapToDto(watch);
    }

    public async Task<WatchDto?> UpdateAsync(int id, UpdateWatchDto dto, int userId, CancellationToken ct = default)
    {
        var watch = await context.Watches
            .FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId, ct);

        if (watch is null) return null;

        watch.Brand = dto.Brand;
        watch.Model = dto.Model;
        watch.MovementType = dto.MovementType;
        watch.CaseSizeMm = dto.CaseSizeMm;
        watch.BandType = dto.BandType;
        watch.BandColor = dto.BandColor;
        watch.PurchaseDate = dto.PurchaseDate;
        watch.PurchasePrice = dto.PurchasePrice;
        watch.Notes = dto.Notes;
        watch.CrystalType = dto.CrystalType;
        watch.CaseShape = dto.CaseShape;
        watch.CrownType = dto.CrownType;
        watch.CalendarType = dto.CalendarType;
        watch.CountryOfOrigin = dto.CountryOfOrigin;
        watch.WaterResistance = dto.WaterResistance;
        watch.LugWidthMm = dto.LugWidthMm;
        watch.DialColor = dto.DialColor;
        watch.BezelType = dto.BezelType;
        watch.PowerReserveHours = dto.PowerReserveHours;
        watch.Sku = dto.Sku;
        watch.SerialNumber = dto.SerialNumber;
        watch.ProductionYear = dto.ProductionYear;
        watch.BatteryType = dto.BatteryType;
        watch.LastBatteryChangedDate = dto.LastBatteryChangedDate;
        watch.LinkUrl = dto.LinkUrl;
        watch.LinkText = dto.LinkText;
        watch.StorageLocation = dto.StorageLocation;
        watch.IsWishList = dto.IsWishList;
        watch.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);

        return MapToDto(watch);
    }

    public async Task<bool> DeleteAsync(int id, int userId, CancellationToken ct = default)
    {
        var watch = await context.Watches
            .FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId, ct);

        if (watch is null) return false;

        context.Watches.Remove(watch);
        await context.SaveChangesAsync(ct);

        return true;
    }

    public async Task<WatchDto?> RecordWearAsync(int id, int userId, CancellationToken ct = default)
    {
        // Retry on concurrency conflict (e.g. rapid double-tap)
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var watch = await context.Watches
                .Include(w => w.Images)
                .FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId, ct);

            if (watch is null) return null;

            watch.TimesWorn++;
            watch.LastWornDate = DateTime.UtcNow;
            watch.UpdatedAt = DateTime.UtcNow;

            var wornAt = DateTime.UtcNow;
            context.WearLogs.Add(new WearLog
            {
                WatchId = watch.Id,
                UserId = userId,
                WornDate = wornAt,
                StartedAt = wornAt,
            });

            try
            {
                await context.SaveChangesAsync(ct);
                return MapToDto(watch);
            }
            catch (DbUpdateConcurrencyException) when (attempt < 2)
            {
                // Reload entity and retry
                foreach (var entry in context.ChangeTracker.Entries())
                    await entry.ReloadAsync(ct);
            }
        }

        return null;
    }

    public async Task<IEnumerable<WearLogDto>> GetWearLogsAsync(int userId, CancellationToken ct = default)
    {
        return await context.WearLogs
            .Include(wl => wl.Watch)
            .ThenInclude(w => w.Images)
            .Where(wl => wl.UserId == userId)
            .OrderByDescending(wl => wl.WornDate)
            .Select(wl => MapWearLogDto(wl))
            .ToListAsync(ct);
    }

    public async Task<bool> DeleteWearLogAsync(int logId, int userId, CancellationToken ct = default)
    {
        var log = await context.WearLogs
            .Include(wl => wl.Watch)
            .FirstOrDefaultAsync(wl => wl.Id == logId && wl.UserId == userId, ct);

        if (log is null) return false;

        // Decrement the watch's TimesWorn counter
        log.Watch.TimesWorn = Math.Max(0, log.Watch.TimesWorn - 1);

        // If this was the most recent wear, update LastWornDate to the next most recent
        var nextMostRecent = await context.WearLogs
            .Where(wl => wl.WatchId == log.WatchId && wl.Id != logId)
            .OrderByDescending(wl => wl.WornDate)
            .FirstOrDefaultAsync(ct);
        log.Watch.LastWornDate = nextMostRecent?.WornDate;

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

        log.WornDate = dto.WornDate;
        log.StartedAt = dto.StartedAt;
        log.EndedAt = dto.EndedAt;

        // Recalculate LastWornDate for the watch
        var latestDate = log.Watch.WearLogs
            .Select(wl => wl.Id == logId ? dto.WornDate : wl.WornDate)
            .OrderByDescending(d => d)
            .FirstOrDefault();
        log.Watch.LastWornDate = latestDate;

        await context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<WatchDto?> RetireAsync(int id, int userId, CancellationToken ct = default)
    {
        var watch = await context.Watches
            .Include(w => w.Images)
            .FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId, ct);

        if (watch is null) return null;

        watch.IsRetired = true;
        watch.RetiredAt = DateTime.UtcNow;
        watch.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(ct);

        return MapToDto(watch);
    }

    public async Task<WatchDto?> UnretireAsync(int id, int userId, CancellationToken ct = default)
    {
        var watch = await context.Watches
            .Include(w => w.Images)
            .FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId, ct);

        if (watch is null) return null;

        watch.IsRetired = false;
        watch.RetiredAt = null;
        watch.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(ct);

        return MapToDto(watch);
    }

    public async Task<WatchDto?> AddManualResaleValueAsync(int watchId, int userId, CreateResaleValueEntryDto dto, CancellationToken ct = default)
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

        // Only replace the denormalized "current" value if this entry is the most recent
        // (a backdated manual entry shouldn't overwrite a newer estimate/entry).
        if (watch.ResaleValueUpdatedAt is null || recordedAt >= watch.ResaleValueUpdatedAt)
        {
            watch.CurrentResaleValue = dto.Value;
            watch.ResaleValueUpdatedAt = recordedAt;
        }
        watch.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);
        return MapToDto(watch);
    }

    public async Task<IEnumerable<ResaleValueEntryDto>> GetResaleHistoryAsync(int watchId, int userId, CancellationToken ct = default)
    {
        return await context.ResaleValueEntries
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
    }

    public async Task<bool> DeleteResaleValueEntryAsync(int entryId, int userId, CancellationToken ct = default)
    {
        var entry = await context.ResaleValueEntries
            .Include(r => r.Watch)
            .FirstOrDefaultAsync(r => r.Id == entryId && r.UserId == userId, ct);

        if (entry is null) return false;

        context.ResaleValueEntries.Remove(entry);

        // If this was the most recent entry, fall back to the next most recent.
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

    private static WatchDto MapToDto(Watch watch) => new()
    {
        Id = watch.Id,
        Brand = watch.Brand,
        Model = watch.Model,
        MovementType = watch.MovementType,
        CaseSizeMm = watch.CaseSizeMm,
        BandType = watch.BandType,
        BandColor = watch.BandColor,
        PurchaseDate = watch.PurchaseDate,
        PurchasePrice = watch.PurchasePrice,
        Notes = watch.Notes,
        AiAnalysis = watch.AiAnalysis,
        LastWornDate = watch.LastWornDate,
        TimesWorn = watch.TimesWorn,
        CurrentResaleValue = watch.CurrentResaleValue,
        ResaleValueUpdatedAt = watch.ResaleValueUpdatedAt,
        CrystalType = watch.CrystalType,
        CaseShape = watch.CaseShape,
        CrownType = watch.CrownType,
        CalendarType = watch.CalendarType,
        CountryOfOrigin = watch.CountryOfOrigin,
        WaterResistance = watch.WaterResistance,
        LugWidthMm = watch.LugWidthMm,
        DialColor = watch.DialColor,
        BezelType = watch.BezelType,
        PowerReserveHours = watch.PowerReserveHours,
        Sku = watch.Sku,
        SerialNumber = watch.SerialNumber,
        ProductionYear = watch.ProductionYear,
        BatteryType = watch.BatteryType,
        LastBatteryChangedDate = watch.LastBatteryChangedDate,
        LinkUrl = watch.LinkUrl,
        LinkText = watch.LinkText,
        StorageLocation = watch.StorageLocation,
        IsWishList = watch.IsWishList,
        IsRetired = watch.IsRetired,
        RetiredAt = watch.RetiredAt,
        ImageUrls = watch.Images.OrderBy(i => i.SortOrder).Select(i => new WatchImageDto
        {
            Id = i.Id,
            Url = $"/uploads/{i.FileName}"
        }).ToList(),
        CreatedAt = watch.CreatedAt,
        UpdatedAt = watch.UpdatedAt
    };

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
            WornDate = log.WornDate,
            StartedAt = log.StartedAt,
            EndedAt = log.EndedAt,
            DurationMinutes = duration,
            WatchImageUrl = log.Watch.Images.OrderBy(i => i.SortOrder).Select(i => $"/uploads/{i.FileName}").FirstOrDefault(),
        };
    }
}
