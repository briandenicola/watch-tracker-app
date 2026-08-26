using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using WatchTracker.Api.Data;
using WatchTracker.Api.DTOs;
using WatchTracker.Api.Models;

namespace WatchTracker.Api.Services;

public class WatchService(AppDbContext context) : IWatchService
{
    private static readonly ConcurrentDictionary<int, SemaphoreSlim> WishlistPriorityLocks = new();

    public async Task<IEnumerable<WatchDto>> GetAllAsync(int userId, bool includeDisposed = false, CancellationToken ct = default)
    {
        var query = context.Watches
            .Include(w => w.Images)
            .Include(w => w.Disposition)
                .ThenInclude(d => d!.ReceivedWatch)
            .Where(w => w.UserId == userId);

        if (!includeDisposed)
            query = query.Where(w => w.Disposition == null);

        return await query
            .Select(w => MapToDto(w))
            .ToListAsync(ct);
    }

    public async Task<WatchDto?> GetByIdAsync(int id, int userId, CancellationToken ct = default)
    {
        var watch = await context.Watches
            .Include(w => w.Images)
            .Include(w => w.Disposition)
                .ThenInclude(d => d!.ReceivedWatch)
            .FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId, ct);

        return watch is null ? null : MapToDto(watch);
    }

    public async Task<WatchDto> CreateAsync(CreateWatchDto dto, int userId, CancellationToken ct = default)
    {
        var priorityLock = dto.IsWishList
            ? WishlistPriorityLocks.GetOrAdd(userId, _ => new SemaphoreSlim(1, 1))
            : null;
        if (priorityLock is not null)
            await priorityLock.WaitAsync(ct);

        try
        {
            int? wishlistPriority = null;
            if (dto.IsWishList)
            {
                wishlistPriority = (await context.Watches
                    .Where(w => w.UserId == userId && w.IsWishList)
                    .MaxAsync(w => (int?)w.WishlistPriority, ct) ?? -1) + 1;
            }

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
                AcquisitionType = dto.AcquisitionType,
                AcquiredFrom = dto.AcquiredFrom,
                AcquisitionSourceUrl = dto.AcquisitionSourceUrl,
                Notes = dto.Notes,
                CrystalType = dto.CrystalType,
                CaseShape = dto.CaseShape,
                CrownType = dto.CrownType,
                CalendarType = dto.CalendarType,
                CountryOfOrigin = dto.CountryOfOrigin,
                WaterResistance = dto.WaterResistance,
                LugWidthMm = dto.LugWidthMm,
                LugToLugMm = dto.LugToLugMm,
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
                WishlistPriority = wishlistPriority,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.Watches.Add(watch);
            await context.SaveChangesAsync(ct);

            return MapToDto(watch);
        }
        finally
        {
            priorityLock?.Release();
        }
    }

    public async Task<WatchDto?> UpdateAsync(int id, UpdateWatchDto dto, int userId, CancellationToken ct = default)
    {
        var watch = await context.Watches
            .Include(w => w.Images)
            .Include(w => w.Disposition)
                .ThenInclude(d => d!.ReceivedWatch)
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
        watch.AcquisitionType = dto.AcquisitionType;
        watch.AcquiredFrom = dto.AcquiredFrom;
        watch.AcquisitionSourceUrl = dto.AcquisitionSourceUrl;
        watch.Notes = dto.Notes;
        watch.CrystalType = dto.CrystalType;
        watch.CaseShape = dto.CaseShape;
        watch.CrownType = dto.CrownType;
        watch.CalendarType = dto.CalendarType;
        watch.CountryOfOrigin = dto.CountryOfOrigin;
        watch.WaterResistance = dto.WaterResistance;
        watch.LugWidthMm = dto.LugWidthMm;
        watch.LugToLugMm = dto.LugToLugMm;
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
        var needsWishlistPriority = dto.IsWishList && !watch.IsWishList;
        var priorityLock = needsWishlistPriority
            ? WishlistPriorityLocks.GetOrAdd(userId, _ => new SemaphoreSlim(1, 1))
            : null;
        if (priorityLock is not null)
            await priorityLock.WaitAsync(ct);

        try
        {
            if (needsWishlistPriority)
            {
                watch.WishlistPriority = (await context.Watches
                    .Where(w => w.UserId == userId && w.IsWishList)
                    .MaxAsync(w => (int?)w.WishlistPriority, ct) ?? -1) + 1;
            }
            else if (!dto.IsWishList)
            {
                watch.WishlistPriority = null;
            }
            watch.IsWishList = dto.IsWishList;
            watch.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync(ct);

            return MapToDto(watch);
        }
        finally
        {
            priorityLock?.Release();
        }
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

    public async Task<WatchDto?> RecordWearAsync(int id, int userId, RecordWearDto? dto = null, CancellationToken ct = default)
    {
        // Retry on concurrency conflict (e.g. rapid double-tap)
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
            // A back-dated wear must not drag LastWornDate backwards off a more
            // recent one, so this keeps whichever is later.
            watch.LastWornDate = watch.LastWornDate is { } previous && previous > wornAt
                ? previous
                : wornAt;
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

        log.WornDate = dto.WornDate.UtcDateTime;
        log.StartedAt = dto.StartedAt?.UtcDateTime;
        log.EndedAt = dto.EndedAt?.UtcDateTime;

        // Recalculate LastWornDate for the watch
        var latestDate = log.Watch.WearLogs
            .Select(wl => wl.Id == logId ? dto.WornDate.UtcDateTime : wl.WornDate)
            .OrderByDescending(d => d)
            .FirstOrDefault();
        log.Watch.LastWornDate = latestDate;

        await context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<WatchDto?> RetireAsync(int id, int userId, CancellationToken ct = default)
    {
        return await SetDispositionAsync(id, userId, new UpdateWatchDispositionDto
        {
            Type = DispositionType.Retired,
            DispositionDate = DateTime.UtcNow,
        }, ct);
    }

    public async Task<WatchDto?> UnretireAsync(int id, int userId, CancellationToken ct = default)
    {
        return await ClearDispositionAsync(id, userId, ct);
    }

    public async Task<WatchDto?> SetDispositionAsync(
        int id,
        int userId,
        UpdateWatchDispositionDto dto,
        CancellationToken ct = default)
    {
        var watch = await context.Watches
            .Include(w => w.Images)
            .Include(w => w.Disposition)
                .ThenInclude(d => d!.ReceivedWatch)
            .FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId, ct);

        if (watch is null) return null;
        if (watch.IsWishList)
            throw new InvalidOperationException("A wish list watch cannot have a disposition.");

        Watch? receivedWatch = null;
        if (dto.Type == DispositionType.Traded && dto.ReceivedWatchId is int receivedWatchId)
        {
            if (receivedWatchId == id)
                throw new InvalidOperationException("A watch cannot be traded for itself.");

            receivedWatch = await context.Watches
                .FirstOrDefaultAsync(
                    w => w.Id == receivedWatchId && w.UserId == userId && !w.IsWishList,
                    ct)
                ?? throw new InvalidOperationException("The selected received watch was not found.");
        }

        var disposition = watch.Disposition ?? new WatchDisposition { WatchId = watch.Id };
        disposition.Type = dto.Type;
        disposition.DispositionDate = dto.DispositionDate;
        disposition.Notes = NullIfWhiteSpace(dto.Notes);
        disposition.SoldTo = dto.Type == DispositionType.Sold ? NullIfWhiteSpace(dto.SoldTo) : null;
        disposition.SalePrice = dto.Type == DispositionType.Sold ? dto.SalePrice : null;
        disposition.ReceivedWatchId = dto.Type == DispositionType.Traded ? dto.ReceivedWatchId : null;
        disposition.ReceivedWatch = receivedWatch;
        disposition.TradeDetails = dto.Type == DispositionType.Traded
            ? NullIfWhiteSpace(dto.TradeDetails)
                ?? (receivedWatch is null ? null : $"{receivedWatch.Brand} {receivedWatch.Model}")
            : null;
        disposition.OtherLabel = dto.Type == DispositionType.Other ? NullIfWhiteSpace(dto.OtherLabel) : null;
        disposition.ReturnReason = dto.Type == DispositionType.Returned ? NullIfWhiteSpace(dto.ReturnReason) : null;
        disposition.ReturnedTo = dto.Type == DispositionType.Returned ? NullIfWhiteSpace(dto.ReturnedTo) : null;
        disposition.RefundAmount = dto.Type == DispositionType.Returned ? dto.RefundAmount : null;

        if (watch.Disposition is null)
        {
            context.WatchDispositions.Add(disposition);
            watch.Disposition = disposition;
        }

        watch.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(ct);

        return MapToDto(watch);
    }

    public async Task<WatchDto?> ClearDispositionAsync(int id, int userId, CancellationToken ct = default)
    {
        var watch = await context.Watches
            .Include(w => w.Images)
            .Include(w => w.Disposition)
            .FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId, ct);

        if (watch is null) return null;

        if (watch.Disposition is not null)
        {
            context.WatchDispositions.Remove(watch.Disposition);
            watch.Disposition = null;
        }

        watch.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(ct);
        return MapToDto(watch);
    }

    public async Task<bool> ReorderWishlistAsync(
        int userId,
        IReadOnlyList<int> watchIds,
        CancellationToken ct = default)
    {
        if (watchIds.Count == 0 || watchIds.Distinct().Count() != watchIds.Count)
            throw new InvalidOperationException("Wishlist order must contain unique watch IDs.");

        var priorityLock = WishlistPriorityLocks.GetOrAdd(userId, _ => new SemaphoreSlim(1, 1));
        await priorityLock.WaitAsync(ct);
        try
        {
            await using var transaction = await context.Database.BeginTransactionAsync(ct);
            var wishlist = await context.Watches
                .Where(w => w.UserId == userId && w.IsWishList)
                .ToListAsync(ct);

            if (wishlist.Count != watchIds.Count
                || wishlist.Select(w => w.Id).ToHashSet().SetEquals(watchIds) is false)
            {
                throw new InvalidOperationException("Wishlist order must include every current wishlist watch.");
            }

            var priorities = watchIds
                .Select((watchId, priority) => (watchId, priority))
                .ToDictionary(item => item.watchId, item => item.priority);

            // Move every row out of the final priority range first so swaps do not
            // collide with the unique (user, priority) index mid-update.
            foreach (var watch in wishlist)
                watch.WishlistPriority = -watch.Id;
            await context.SaveChangesAsync(ct);

            foreach (var watch in wishlist)
            {
                watch.WishlistPriority = priorities[watch.Id];
                watch.UpdatedAt = DateTime.UtcNow;
            }

            await context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return true;
        }
        finally
        {
            priorityLock.Release();
        }
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
        AcquisitionType = watch.AcquisitionType,
        AcquiredFrom = watch.AcquiredFrom,
        AcquisitionSourceUrl = watch.AcquisitionSourceUrl,
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
        LugToLugMm = watch.LugToLugMm,
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
        MarketplaceCurrency = watch.MarketplaceCurrency,
        MarketplaceObservedAt = watch.MarketplaceObservedAt,
        StorageLocation = watch.StorageLocation,
        IsWishList = watch.IsWishList,
        WishlistPriority = watch.WishlistPriority,
        Disposition = watch.Disposition is null ? null : new WatchDispositionDto
        {
            Type = watch.Disposition.Type,
            DispositionDate = watch.Disposition.DispositionDate,
            Notes = watch.Disposition.Notes,
            SoldTo = watch.Disposition.SoldTo,
            SalePrice = watch.Disposition.SalePrice,
            ReceivedWatchId = watch.Disposition.ReceivedWatchId,
            ReceivedWatchName = watch.Disposition.ReceivedWatch is null
                ? null
                : $"{watch.Disposition.ReceivedWatch.Brand} {watch.Disposition.ReceivedWatch.Model}",
            TradeDetails = watch.Disposition.TradeDetails,
            OtherLabel = watch.Disposition.OtherLabel,
            ReturnReason = watch.Disposition.ReturnReason,
            ReturnedTo = watch.Disposition.ReturnedTo,
            RefundAmount = watch.Disposition.RefundAmount,
        },
        ImageUrls = watch.Images.OrderBy(i => i.SortOrder).Select(i => new WatchImageDto
        {
            Id = i.Id,
            Url = $"/uploads/{i.FileName}"
        }).ToList(),
        CreatedAt = watch.CreatedAt,
        UpdatedAt = watch.UpdatedAt
    };

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

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
            StartedAt = log.StartedAt is null
                ? null
                : DateTime.SpecifyKind(log.StartedAt.Value, DateTimeKind.Utc),
            EndedAt = log.EndedAt is null
                ? null
                : DateTime.SpecifyKind(log.EndedAt.Value, DateTimeKind.Utc),
            DurationMinutes = duration,
            WatchImageUrl = log.Watch.Images.OrderBy(i => i.SortOrder).Select(i => $"/uploads/{i.FileName}").FirstOrDefault(),
        };
    }
}
