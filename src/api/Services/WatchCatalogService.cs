using Microsoft.EntityFrameworkCore;
using WatchTracker.Api.Data;
using WatchTracker.Api.DTOs;
using WatchTracker.Api.Models;

namespace WatchTracker.Api.Services;

public class WatchCatalogService(
    AppDbContext context,
    IUploadStorage storage,
    ILogger<WatchCatalogService> logger) : IWatchCatalogService
{
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
            .Select(w => WatchDtoMapper.Map(w))
            .ToListAsync(ct);
    }

    public async Task<WatchDto?> GetByIdAsync(int id, int userId, CancellationToken ct = default)
    {
        var watch = await context.Watches
            .Include(w => w.Images)
            .Include(w => w.Disposition)
                .ThenInclude(d => d!.ReceivedWatch)
            .FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId, ct);

        return watch is null ? null : WatchDtoMapper.Map(watch);
    }

    public async Task<WatchDto> CreateAsync(CreateWatchDto dto, int userId, CancellationToken ct = default)
    {
        var priorityLock = dto.IsWishList ? WishlistPriorityLocks.ForUser(userId) : null;
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
                UserId = userId,
                WishlistPriority = wishlistPriority,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            WatchFieldMapper.Apply(watch, dto);

            context.Watches.Add(watch);
            await context.SaveChangesAsync(ct);
            return WatchDtoMapper.Map(watch);
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

        var needsWishlistPriority = dto.IsWishList && !watch.IsWishList;
        var priorityLock = needsWishlistPriority ? WishlistPriorityLocks.ForUser(userId) : null;
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
                watch.PriceAlertEnabled = false;
                watch.PriceAlertTarget = null;
            }

            WatchFieldMapper.Apply(watch, dto);
            watch.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync(ct);
            return WatchDtoMapper.Map(watch);
        }
        finally
        {
            priorityLock?.Release();
        }
    }

    public async Task<bool> DeleteAsync(int id, int userId, CancellationToken ct = default)
    {
        var watch = await context.Watches
            .Include(w => w.Images)
            .FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId, ct);
        if (watch is null) return false;

        // Read the names before the cascade takes the rows that point at them.
        var fileNames = watch.Images.Select(i => i.FileName).ToList();

        context.Watches.Remove(watch);
        await context.SaveChangesAsync(ct);

        // Files go only once the delete has committed. A file removed ahead of a
        // failed save would leave a live row pointing at nothing, which is worse
        // than the leftover file this is here to prevent.
        foreach (var fileName in fileNames)
        {
            if (!storage.TryGetFilePath(fileName, out var path)) continue;

            try
            {
                File.Delete(path);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                logger.LogError(
                    ex,
                    "Could not delete image file {FileName} belonging to deleted watch {WatchId}.",
                    fileName,
                    id);
            }
        }

        return true;
    }
}
