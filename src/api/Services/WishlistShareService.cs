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
        var items = await GetItemsAsync(share.UserId, ct);

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

    public async Task<IReadOnlyList<WishlistShareUserDto>> SearchUsersAsync(
        int ownerUserId,
        string query,
        CancellationToken ct = default)
    {
        var normalized = query.Trim();
        if (normalized.Length < 2) return [];

        return await context.Users
            .AsNoTracking()
            .Where(user => user.Id != ownerUserId)
            .Where(user => user.Username.Contains(normalized))
            .OrderBy(user => user.Username)
            .Take(10)
            .Select(user => new WishlistShareUserDto
            {
                Id = user.Id,
                Username = user.Username
            })
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<WishlistUserShareDto>> GetUserSharesAsync(
        int ownerUserId,
        CancellationToken ct = default) =>
        await context.WishlistUserShares
            .AsNoTracking()
            .Where(share => share.OwnerUserId == ownerUserId)
            .OrderBy(share => share.RecipientUser.Username)
            .Select(share => new WishlistUserShareDto
            {
                Id = share.Id,
                RecipientUserId = share.RecipientUserId,
                RecipientUsername = share.RecipientUser.Username,
                IncludePrices = share.IncludePrices,
                CreatedAt = share.CreatedAt,
                LastViewedAt = share.LastViewedAt,
                ViewCount = share.ViewCount
            })
            .ToListAsync(ct);

    public async Task<WishlistUserShareDto?> ShareWithUserAsync(
        int ownerUserId,
        CreateWishlistUserShareDto request,
        CancellationToken ct = default)
    {
        if (request.RecipientUserId == ownerUserId) return null;
        var recipientExists = await context.Users
            .AnyAsync(user => user.Id == request.RecipientUserId, ct);
        if (!recipientExists) return null;

        var share = await context.WishlistUserShares
            .Include(existing => existing.RecipientUser)
            .FirstOrDefaultAsync(existing =>
                existing.OwnerUserId == ownerUserId
                && existing.RecipientUserId == request.RecipientUserId, ct);
        if (share is null)
        {
            share = new WishlistUserShare
            {
                OwnerUserId = ownerUserId,
                RecipientUserId = request.RecipientUserId,
                IncludePrices = request.IncludePrices
            };
            context.WishlistUserShares.Add(share);
        }
        else
        {
            share.IncludePrices = request.IncludePrices;
        }

        await context.SaveChangesAsync(ct);
        if (share.RecipientUser is null)
            await context.Entry(share).Reference(existing => existing.RecipientUser).LoadAsync(ct);
        return ToUserShareDto(share);
    }

    public async Task<bool> RevokeUserShareAsync(
        int ownerUserId,
        int shareId,
        CancellationToken ct = default)
    {
        var share = await context.WishlistUserShares.FirstOrDefaultAsync(
            existing => existing.Id == shareId && existing.OwnerUserId == ownerUserId,
            ct);
        if (share is null) return false;

        context.WishlistUserShares.Remove(share);
        await context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<IReadOnlyList<ReceivedWishlistShareDto>> GetReceivedSharesAsync(
        int recipientUserId,
        CancellationToken ct = default) =>
        await context.WishlistUserShares
            .AsNoTracking()
            .Where(share => share.RecipientUserId == recipientUserId)
            .OrderByDescending(share => share.CreatedAt)
            .Select(share => new ReceivedWishlistShareDto
            {
                Id = share.Id,
                OwnerName = share.OwnerUser.Username,
                IncludesPrices = share.IncludePrices,
                SharedAt = share.CreatedAt,
                ItemCount = context.Watches.Count(watch =>
                    watch.UserId == share.OwnerUserId
                    && watch.IsWishList
                    && watch.Disposition == null)
            })
            .ToListAsync(ct);

    public async Task<SharedWishlistDto?> ViewReceivedShareAsync(
        int shareId,
        int recipientUserId,
        CancellationToken ct = default)
    {
        var share = await context.WishlistUserShares
            .Include(existing => existing.OwnerUser)
            .FirstOrDefaultAsync(existing =>
                existing.Id == shareId && existing.RecipientUserId == recipientUserId,
                ct);
        if (share is null) return null;

        var items = await GetItemsAsync(share.OwnerUserId, ct);
        share.ViewCount++;
        share.LastViewedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(ct);

        return new SharedWishlistDto
        {
            OwnerName = share.OwnerUser.Username,
            IncludesPrices = share.IncludePrices,
            SharedAt = share.CreatedAt,
            Items = items.Select(watch => ToItemDto(watch, share.IncludePrices)).ToList()
        };
    }

    private async Task<List<Watch>> GetItemsAsync(int ownerUserId, CancellationToken ct) =>
        await context.Watches
            .Where(w => w.UserId == ownerUserId && w.IsWishList && w.Disposition == null)
            .Include(w => w.Images)
            .OrderBy(w => w.WishlistPriority == null)
            .ThenBy(w => w.WishlistPriority)
            .ThenByDescending(w => w.CreatedAt)
            .ToListAsync(ct);

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

    private static WishlistUserShareDto ToUserShareDto(WishlistUserShare share) => new()
    {
        Id = share.Id,
        RecipientUserId = share.RecipientUserId,
        RecipientUsername = share.RecipientUser.Username,
        IncludePrices = share.IncludePrices,
        CreatedAt = share.CreatedAt,
        LastViewedAt = share.LastViewedAt,
        ViewCount = share.ViewCount
    };
}
