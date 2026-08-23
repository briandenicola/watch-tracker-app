using System.Security.Cryptography;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using WatchTracker.Api.Data;
using WatchTracker.Api.DTOs;
using WatchTracker.Api.Models;

namespace WatchTracker.Api.Services;

public class WatchShareService(AppDbContext context) : IWatchShareService
{
    /// 32 bytes — the link is the whole credential, so it is sized like one.
    private const int TokenBytes = 32;

    /// Longer than any token this issues; anything else cannot be a real link.
    private const int MaxTokenLength = 100;

    public async Task<WatchShareDto?> GetAsync(int watchId, int userId, CancellationToken ct = default)
    {
        var share = await context.WatchShares
            .FirstOrDefaultAsync(s => s.WatchId == watchId && s.UserId == userId, ct);

        return share is null ? null : ToDto(share);
    }

    public async Task<WatchShareDto?> CreateAsync(int watchId, int userId, CancellationToken ct = default)
    {
        var watchExists = await context.Watches.AnyAsync(w => w.Id == watchId && w.UserId == userId, ct);
        if (!watchExists) return null;

        var existing = await context.WatchShares
            .FirstOrDefaultAsync(s => s.WatchId == watchId && s.UserId == userId, ct);
        if (existing is not null) return ToDto(existing);

        var share = new WatchShare
        {
            WatchId = watchId,
            UserId = userId,
            Token = GenerateToken()
        };

        context.WatchShares.Add(share);
        await context.SaveChangesAsync(ct);

        return ToDto(share);
    }

    public async Task<bool> RevokeAsync(int watchId, int userId, CancellationToken ct = default)
    {
        var share = await context.WatchShares
            .FirstOrDefaultAsync(s => s.WatchId == watchId && s.UserId == userId, ct);
        if (share is null) return false;

        context.WatchShares.Remove(share);
        await context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<SharedWatchDto?> ViewAsync(string token, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > MaxTokenLength) return null;

        var share = await context.WatchShares
            .Include(s => s.Watch)
            .ThenInclude(w => w.Images)
            .FirstOrDefaultAsync(s => s.Token == token, ct);

        if (share is null) return null;

        share.ViewCount++;
        share.LastViewedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(ct);

        return ToSharedDto(share);
    }

    private static WatchShareDto ToDto(WatchShare share) => new()
    {
        Token = share.Token,
        Path = $"/s/{share.Token}",
        CreatedAt = share.CreatedAt,
        LastViewedAt = share.LastViewedAt,
        ViewCount = share.ViewCount
    };

    /// <summary>
    /// Copies across only the fields <see cref="SharedWatchDto"/> declares. Every
    /// new line here is a deliberate decision to publish something, which is the
    /// point of mapping by hand rather than handing over the whole watch.
    /// </summary>
    private static SharedWatchDto ToSharedDto(WatchShare share)
    {
        var watch = share.Watch;

        return new SharedWatchDto
        {
            Brand = watch.Brand,
            Model = watch.Model,
            Sku = watch.Sku,
            MovementType = watch.MovementType,
            CaseSizeMm = watch.CaseSizeMm,
            CaseShape = watch.CaseShape,
            CrystalType = watch.CrystalType,
            BezelType = watch.BezelType,
            CrownType = watch.CrownType,
            CalendarType = watch.CalendarType,
            DialColor = watch.DialColor,
            BandType = watch.BandType,
            BandColor = watch.BandColor,
            LugWidthMm = watch.LugWidthMm,
            WaterResistance = watch.WaterResistance,
            PowerReserveHours = watch.PowerReserveHours,
            BatteryType = watch.BatteryType,
            ProductionYear = watch.ProductionYear,
            CountryOfOrigin = watch.CountryOfOrigin,
            LinkUrl = watch.LinkUrl,
            LinkText = watch.LinkText,
            IsWishList = watch.IsWishList,
            SharedAt = share.CreatedAt,
            ImageUrls = watch.Images
                .OrderBy(i => i.SortOrder)
                .Select(i => new WatchImageDto { Id = i.Id, Url = $"/uploads/{i.FileName}" })
                .ToList()
        };
    }

    private static string GenerateToken() =>
        WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(TokenBytes));
}
