using Microsoft.EntityFrameworkCore;
using WatchTracker.Api.Data;
using WatchTracker.Api.DTOs;
using WatchTracker.Api.Models;

namespace WatchTracker.Api.Services;

public class WatchShareService(AppDbContext context, IAppSettingsService appSettings) : IWatchShareService
{
    public async Task<WatchShareDto?> GetAsync(int watchId, int userId, CancellationToken ct = default)
    {
        var share = await context.WatchShares
            .FirstOrDefaultAsync(s => s.WatchId == watchId && s.UserId == userId, ct);

        return share is null ? null : ToDto(share, await GetShareBaseUrlAsync());
    }

    public async Task<WatchShareDto?> CreateAsync(int watchId, int userId, CancellationToken ct = default)
    {
        var watchExists = await context.Watches.AnyAsync(w => w.Id == watchId && w.UserId == userId, ct);
        if (!watchExists) return null;

        var existing = await context.WatchShares
            .FirstOrDefaultAsync(s => s.WatchId == watchId && s.UserId == userId, ct);
        if (existing is not null) return ToDto(existing, await GetShareBaseUrlAsync());

        var share = new WatchShare
        {
            WatchId = watchId,
            UserId = userId,
            Token = ShareTokens.Generate()
        };

        context.WatchShares.Add(share);
        await context.SaveChangesAsync(ct);

        return ToDto(share, await GetShareBaseUrlAsync());
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
        if (!ShareTokens.IsWellFormed(token)) return null;

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

    /// <summary>
    /// The address to hand out, when the app is reachable somewhere other than
    /// where its owner administers it — an internal hostname is no use to the
    /// friend the link is for. Empty (the default) means the client falls back
    /// to whatever origin it is being viewed on.
    /// </summary>
    private async Task<string?> GetShareBaseUrlAsync()
    {
        var configured = await appSettings.GetAsync(AppSettingsService.Keys.ShareLinkBaseUrl);
        if (string.IsNullOrWhiteSpace(configured)) return null;

        // A typo here would produce links that quietly go nowhere, so anything
        // that is not an absolute web address is treated as unset.
        if (!Uri.TryCreate(configured.Trim(), UriKind.Absolute, out var parsed)) return null;
        if (parsed.Scheme != Uri.UriSchemeHttp && parsed.Scheme != Uri.UriSchemeHttps) return null;

        return parsed.GetLeftPart(UriPartial.Authority) + parsed.AbsolutePath.TrimEnd('/');
    }

    private static WatchShareDto ToDto(WatchShare share, string? baseUrl) => new()
    {
        Token = share.Token,
        Url = baseUrl is null ? null : $"{baseUrl}/s/{share.Token}",
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
            LugToLugMm = watch.LugToLugMm,
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

}
