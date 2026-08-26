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
            var existing = await context.Watches
                .Where(w => w.UserId == userId && w.IsWishList && w.Disposition == null)
                .ToListAsync(ct);

            var duplicate = FindDuplicate(existing, card);
            if (duplicate is not null)
            {
                return new AdvisorWishlistActionResultDto
                {
                    Added = false,
                    WatchId = duplicate.Id,
                    Message = "This recommendation is already on your wishlist."
                };
            }

            var watch = ToWishlistWatch(card, userId, note, existing);
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

    /// <summary>
    /// The same watch can arrive as the same listing, as the same link, or as the
    /// same watch typed in by hand, so all three are checked.
    /// </summary>
    private static Watch? FindDuplicate(IEnumerable<Watch> wishlist, AdvisorRecommendationCardDto card)
    {
        var normalizedBrand = Normalize(card.Brand);
        var normalizedModel = Normalize(card.Model);
        var normalizedReference = Normalize(card.ReferenceNumber);

        return wishlist.FirstOrDefault(w =>
            (string.Equals(w.MarketplaceProvider, card.Provider, StringComparison.OrdinalIgnoreCase)
                && string.Equals(w.MarketplaceItemId, card.ProviderItemId, StringComparison.OrdinalIgnoreCase))
            || string.Equals(NormalizeUrl(w.LinkUrl), NormalizeUrl(card.ItemUrl), StringComparison.OrdinalIgnoreCase)
            || (normalizedBrand.Length > 0
                && normalizedModel.Length > 0
                && Normalize(w.Brand) == normalizedBrand
                && Normalize(w.Model) == normalizedModel
                && (normalizedReference.Length == 0 || Normalize(w.Sku) == normalizedReference)));
    }

    private static Watch ToWishlistWatch(
        AdvisorRecommendationCardDto card,
        int userId,
        string note,
        IReadOnlyCollection<Watch> existing) => new()
    {
        UserId = userId,
        Brand = TrimTo(card.Brand ?? card.Provider!, 200),
        Model = TrimTo(card.Model ?? card.Title, 200),
        MovementType = MovementType.Unknown,
        IsWishList = true,
        WishlistPriority = (existing.Max(w => w.WishlistPriority) ?? -1) + 1,
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
