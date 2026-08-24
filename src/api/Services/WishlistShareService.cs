using Microsoft.EntityFrameworkCore;
using WatchTracker.Api.Data;
using WatchTracker.Api.DTOs;
using WatchTracker.Api.Models;

namespace WatchTracker.Api.Services;

public class WishlistShareService(AppDbContext context, IAppSettingsService appSettings) : IWishlistShareService
{
    public async Task<WishlistShareDto?> GetAsync(int userId, CancellationToken ct = default)
    {
        var share = await context.WishlistShares.FirstOrDefaultAsync(s => s.UserId == userId, ct);
        return share is null ? null : ToDto(share, await GetShareBaseUrlAsync());
    }

    public async Task<WishlistShareDto> CreateAsync(
        int userId, UpdateWishlistShareDto options, CancellationToken ct = default)
    {
        var share = await context.WishlistShares.FirstOrDefaultAsync(s => s.UserId == userId, ct);

        if (share is null)
        {
            share = new WishlistShare
            {
                UserId = userId,
                Token = ShareTokens.Generate(),
                IncludePrices = options.IncludePrices
            };
            context.WishlistShares.Add(share);
        }
        else
        {
            // Sharing again should not invalidate a link already handed out.
            share.IncludePrices = options.IncludePrices;
        }

        await context.SaveChangesAsync(ct);
        return ToDto(share, await GetShareBaseUrlAsync());
    }

    public async Task<WishlistShareDto?> UpdateAsync(
        int userId, UpdateWishlistShareDto options, CancellationToken ct = default)
    {
        var share = await context.WishlistShares.FirstOrDefaultAsync(s => s.UserId == userId, ct);
        if (share is null) return null;

        share.IncludePrices = options.IncludePrices;
        await context.SaveChangesAsync(ct);

        return ToDto(share, await GetShareBaseUrlAsync());
    }

    public async Task<bool> RevokeAsync(int userId, CancellationToken ct = default)
    {
        var share = await context.WishlistShares.FirstOrDefaultAsync(s => s.UserId == userId, ct);
        if (share is null) return false;

        context.WishlistShares.Remove(share);
        await context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<SharedWishlistDto?> ViewAsync(string token, CancellationToken ct = default)
    {
        if (!ShareTokens.IsWellFormed(token)) return null;

        var share = await context.WishlistShares
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Token == token, ct);

        if (share is null) return null;

        // Disposed-of wish list entries are not on the list any more, and a
        // retired one was never on it, so both stay out of the public view.
        var items = await context.Watches
            .Where(w => w.UserId == share.UserId && w.IsWishList && w.Disposition == null)
            .Include(w => w.Images)
            .OrderBy(w => w.WishlistPriority == null)
            .ThenBy(w => w.WishlistPriority)
            .ThenByDescending(w => w.CreatedAt)
            .ToListAsync(ct);

        share.ViewCount++;
        share.LastViewedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(ct);

        return new SharedWishlistDto
        {
            OwnerName = share.User.Username,
            IncludesPrices = share.IncludePrices,
            SharedAt = share.CreatedAt,
            Items = items.Select(w => ToItemDto(w, share.IncludePrices)).ToList()
        };
    }

    /// <summary>
    /// Copies across only the fields <see cref="SharedWishlistItemDto"/> declares,
    /// by hand, for the same reason the single-watch share does: publishing has
    /// to be a decision rather than a default.
    /// </summary>
    private static SharedWishlistItemDto ToItemDto(Watch watch, bool includePrices) => new()
    {
        Brand = watch.Brand,
        Model = watch.Model,
        Sku = watch.Sku,
        MovementType = watch.MovementType,
        CaseSizeMm = watch.CaseSizeMm,
        CaseShape = watch.CaseShape,
        DialColor = watch.DialColor,
        BandType = watch.BandType,
        BandColor = watch.BandColor,
        WaterResistance = watch.WaterResistance,
        CountryOfOrigin = watch.CountryOfOrigin,
        LinkUrl = watch.LinkUrl,
        LinkText = watch.LinkText,
        TargetPrice = includePrices ? watch.PurchasePrice : null,
        ImageUrls = watch.Images
            .OrderBy(i => i.SortOrder)
            .Select(i => new WatchImageDto { Id = i.Id, Url = $"/uploads/{i.FileName}" })
            .ToList()
    };

    /// <summary>
    /// The address to hand out, when the app is reachable somewhere other than
    /// where its owner administers it. Empty (the default) means the client
    /// falls back to whatever origin it is being viewed on.
    /// </summary>
    private async Task<string?> GetShareBaseUrlAsync()
    {
        var configured = await appSettings.GetAsync(AppSettingsService.Keys.ShareLinkBaseUrl);
        if (string.IsNullOrWhiteSpace(configured)) return null;

        if (!Uri.TryCreate(configured.Trim(), UriKind.Absolute, out var parsed)) return null;
        if (parsed.Scheme != Uri.UriSchemeHttp && parsed.Scheme != Uri.UriSchemeHttps) return null;

        return parsed.GetLeftPart(UriPartial.Authority) + parsed.AbsolutePath.TrimEnd('/');
    }

    private static WishlistShareDto ToDto(WishlistShare share, string? baseUrl) => new()
    {
        Token = share.Token,
        Url = baseUrl is null ? null : $"{baseUrl}/w/{share.Token}",
        Path = $"/w/{share.Token}",
        IncludePrices = share.IncludePrices,
        CreatedAt = share.CreatedAt,
        LastViewedAt = share.LastViewedAt,
        ViewCount = share.ViewCount
    };
}
