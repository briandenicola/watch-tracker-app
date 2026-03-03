using Microsoft.EntityFrameworkCore;
using WatchTracker.Api.Data;
using WatchTracker.Api.DTOs;
using WatchTracker.Api.Models;

namespace WatchTracker.Api.Services;

public class WatchService(AppDbContext context) : IWatchService
{
    public async Task<IEnumerable<WatchDto>> GetAllAsync(int userId)
    {
        return await context.Watches
            .Include(w => w.Images)
            .Where(w => w.UserId == userId)
            .Select(w => MapToDto(w))
            .ToListAsync();
    }

    public async Task<WatchDto?> GetByIdAsync(int id, int userId)
    {
        var watch = await context.Watches
            .Include(w => w.Images)
            .FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId);

        return watch is null ? null : MapToDto(watch);
    }

    public async Task<WatchDto> CreateAsync(CreateWatchDto dto, int userId)
    {
        var watch = new Watch
        {
            Brand = dto.Brand,
            Model = dto.Model,
            MovementType = dto.MovementType,
            CaseSizeMm = dto.CaseSizeMm,
            BandType = dto.BandType,
            PurchaseDate = dto.PurchaseDate,
            PurchasePrice = dto.PurchasePrice,
            Notes = dto.Notes,
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Watches.Add(watch);
        await context.SaveChangesAsync();

        return MapToDto(watch);
    }

    public async Task<WatchDto?> UpdateAsync(int id, UpdateWatchDto dto, int userId)
    {
        var watch = await context.Watches
            .FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId);

        if (watch is null) return null;

        watch.Brand = dto.Brand;
        watch.Model = dto.Model;
        watch.MovementType = dto.MovementType;
        watch.CaseSizeMm = dto.CaseSizeMm;
        watch.BandType = dto.BandType;
        watch.PurchaseDate = dto.PurchaseDate;
        watch.PurchasePrice = dto.PurchasePrice;
        watch.Notes = dto.Notes;
        watch.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return MapToDto(watch);
    }

    public async Task<bool> DeleteAsync(int id, int userId)
    {
        var watch = await context.Watches
            .FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId);

        if (watch is null) return false;

        context.Watches.Remove(watch);
        await context.SaveChangesAsync();

        return true;
    }

    public async Task<WatchDto?> RecordWearAsync(int id, int userId)
    {
        var watch = await context.Watches
            .Include(w => w.Images)
            .FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId);

        if (watch is null) return null;

        watch.TimesWorn++;
        watch.LastWornDate = DateTime.UtcNow;
        watch.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

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
        PurchaseDate = watch.PurchaseDate,
        PurchasePrice = watch.PurchasePrice,
        Notes = watch.Notes,
        AiAnalysis = watch.AiAnalysis,
        LastWornDate = watch.LastWornDate,
        TimesWorn = watch.TimesWorn,
        ImageUrls = watch.Images.OrderBy(i => i.SortOrder).Select(i => new WatchImageDto
        {
            Id = i.Id,
            Url = $"/uploads/{i.FileName}"
        }).ToList(),
        CreatedAt = watch.CreatedAt,
        UpdatedAt = watch.UpdatedAt
    };
}
