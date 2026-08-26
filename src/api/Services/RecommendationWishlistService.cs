using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using WatchTracker.Api.Data;
using WatchTracker.Api.DTOs;
using WatchTracker.Api.Models;

namespace WatchTracker.Api.Services;

/// <summary>
/// Turns a recommendation card into a wish list watch. Every source of cards —
/// the Collection Advisor's chat recommendations today, others later — goes
/// through here, so duplicate detection has one implementation rather than
/// several that drift.
/// </summary>
public class RecommendationWishlistService(AppDbContext context) : IRecommendationWishlistService
{
    // Two adds racing each other would both read an empty wish list and both
    // write, so the read and the write are held together per user.
    private static readonly ConcurrentDictionary<int, SemaphoreSlim> WishlistLocks = new();

    public async Task<AdvisorWishlistActionResultDto?> AddAsync(
        AdvisorRecommendationCardDto card,
        int userId,
        string note,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(card.ItemUrl)) return null;

        var wishlistLock = WishlistLocks.GetOrAdd(userId, _ => new SemaphoreSlim(1, 1));
        await wishlistLock.WaitAsync(ct);
        try
        {
            // Every watch, not just the wish list: the database keeps one row per
            // user per marketplace listing whatever became of it, so a listing
            // already recorded on an owned or disposed-of watch has to be reported
            // rather than inserted a second time.
            var owned = await context.Watches
                .Where(w => w.UserId == userId)
                .Select(w => new ExistingWatch(
                    w.Id,
                    w.Brand,
                    w.Model,
                    w.Sku,
                    w.LinkUrl,
                    w.MarketplaceProvider,
                    w.MarketplaceItemId,
                    w.IsWishList,
                    w.Disposition == null,
                    w.WishlistPriority))
                .ToListAsync(ct);

            var sameListing = owned.FirstOrDefault(w => IsSameListing(w, card));
            if (sameListing is not null)
            {
                return new AdvisorWishlistActionResultDto
                {
                    Added = false,
                    WatchId = sameListing.Id,
                    Message = sameListing is { IsWishList: true, IsActive: true }
                        ? "This recommendation is already on your wishlist."
                        : "You already recorded this listing on another watch."
                };
            }

            var wishlist = owned.Where(w => w is { IsWishList: true, IsActive: true }).ToList();
            var duplicate = FindDuplicate(wishlist, card);
            if (duplicate is not null)
            {
                return new AdvisorWishlistActionResultDto
                {
                    Added = false,
                    WatchId = duplicate.Id,
                    Message = "This recommendation is already on your wishlist."
                };
            }

            var watch = ToWishlistWatch(card, userId, note, wishlist);
            context.Watches.Add(watch);
            await context.SaveChangesAsync(ct);
            return new AdvisorWishlistActionResultDto
            {
                Added = true,
                WatchId = watch.Id,
                Message = "Added to your wishlist."
            };
        }
        finally
        {
            wishlistLock.Release();
        }
    }

    /// <summary>The same listing, wherever in the user's watches it landed.</summary>
    private static bool IsSameListing(ExistingWatch watch, AdvisorRecommendationCardDto card) =>
        !string.IsNullOrWhiteSpace(card.Provider)
        && !string.IsNullOrWhiteSpace(card.ProviderItemId)
        && string.Equals(watch.MarketplaceProvider, card.Provider, StringComparison.OrdinalIgnoreCase)
        && string.Equals(watch.MarketplaceItemId, card.ProviderItemId, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// The same watch can also arrive as the same link, or as the same watch typed
    /// in by hand, neither of which the listing check above catches.
    /// </summary>
    private static ExistingWatch? FindDuplicate(
        IEnumerable<ExistingWatch> wishlist,
        AdvisorRecommendationCardDto card)
    {
        var normalizedBrand = Normalize(card.Brand);
        var normalizedModel = Normalize(card.Model);
        var normalizedReference = Normalize(card.ReferenceNumber);

        return wishlist.FirstOrDefault(w =>
            string.Equals(NormalizeUrl(w.LinkUrl), NormalizeUrl(card.ItemUrl), StringComparison.OrdinalIgnoreCase)
            || (normalizedBrand.Length > 0
                && normalizedModel.Length > 0
                && Normalize(w.Brand) == normalizedBrand
                && Normalize(w.Model) == normalizedModel
                && (normalizedReference.Length == 0 || Normalize(w.Sku) == normalizedReference)));
    }

    /// <summary>Just the fields duplicate detection and priority assignment read.</summary>
    private sealed record ExistingWatch(
        int Id,
        string Brand,
        string Model,
        string? Sku,
        string? LinkUrl,
        string? MarketplaceProvider,
        string? MarketplaceItemId,
        bool IsWishList,
        bool IsActive,
        int? WishlistPriority);

    private static Watch ToWishlistWatch(
        AdvisorRecommendationCardDto card,
        int userId,
        string note,
        IReadOnlyCollection<ExistingWatch> wishlist) => new()
    {
        UserId = userId,
        Brand = TrimTo(card.Brand ?? card.Provider!, 200),
        Model = TrimTo(card.Model ?? card.Title, 200),
        MovementType = MovementType.Unknown,
        IsWishList = true,
        WishlistPriority = (wishlist.Max(w => w.WishlistPriority) ?? -1) + 1,
        PurchasePrice = card.TotalPrice ?? card.Price,
        Sku = TrimNullableTo(card.ReferenceNumber, 100),
        LinkUrl = card.ItemUrl,
        LinkText = TrimTo(card.Title, 200),
        AcquiredFrom = TrimNullableTo(card.Provider, 200),
        MarketplaceProvider = card.Provider,
        MarketplaceItemId = card.ProviderItemId,
        MarketplaceCurrency = card.Currency,
        MarketplaceObservedAt = card.ObservedAt,
        Notes = note,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    private static string Normalize(string? value) =>
        new((value ?? "").Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());

    private static string NormalizeUrl(string? value) =>
        (value ?? "").Trim().TrimEnd('/');

    private static string TrimTo(string value, int length) =>
        value.Length <= length ? value : value[..length];

    private static string? TrimNullableTo(string? value, int length) =>
        string.IsNullOrWhiteSpace(value) ? null : TrimTo(value.Trim(), length);
}
