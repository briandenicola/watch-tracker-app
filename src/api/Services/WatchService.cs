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
            SerialNumber = dto.SerialNumber,
            BatteryType = dto.BatteryType,
            LinkUrl = dto.LinkUrl,
            LinkText = dto.LinkText,
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
        watch.SerialNumber = dto.SerialNumber;
        watch.BatteryType = dto.BatteryType;
        watch.LinkUrl = dto.LinkUrl;
        watch.LinkText = dto.LinkText;
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

            context.WearLogs.Add(new WearLog
            {
                WatchId = watch.Id,
                UserId = userId,
                WornDate = DateTime.UtcNow,
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
            .Where(wl => wl.UserId == userId)
            .OrderByDescending(wl => wl.WornDate)
            .Select(wl => new WearLogDto
            {
                Id = wl.Id,
                WatchId = wl.WatchId,
                WatchBrand = wl.Watch.Brand,
                WatchModel = wl.Watch.Model,
                WornDate = wl.WornDate,
            })
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

    public async Task<bool> UpdateWearLogDateAsync(int logId, int userId, DateTime newDate, CancellationToken ct = default)
    {
        var log = await context.WearLogs
            .Include(wl => wl.Watch)
            .ThenInclude(w => w.WearLogs)
            .FirstOrDefaultAsync(wl => wl.Id == logId && wl.UserId == userId, ct);

        if (log is null) return false;

        log.WornDate = newDate;

        // Recalculate LastWornDate for the watch
        var latestDate = log.Watch.WearLogs
            .Select(wl => wl.Id == logId ? newDate : wl.WornDate)
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
        SerialNumber = watch.SerialNumber,
        BatteryType = watch.BatteryType,
        LinkUrl = watch.LinkUrl,
        LinkText = watch.LinkText,
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
}
