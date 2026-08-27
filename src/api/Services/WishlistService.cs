using Microsoft.EntityFrameworkCore;
using WatchTracker.Api.Data;

namespace WatchTracker.Api.Services;

public class WishlistService(AppDbContext context) : IWishlistService
{
    public async Task<bool> ReorderAsync(int userId, IReadOnlyList<int> watchIds, CancellationToken ct = default)
    {
        if (watchIds.Count == 0 || watchIds.Distinct().Count() != watchIds.Count)
            throw new InvalidOperationException("Wishlist order must contain unique watch IDs.");

        var priorityLock = WishlistPriorityLocks.ForUser(userId);
        await priorityLock.WaitAsync(ct);
        try
        {
            await using var transaction = await context.Database.BeginTransactionAsync(ct);
            var wishlist = await context.Watches
                .Where(w => w.UserId == userId && w.IsWishList)
                .ToListAsync(ct);

            if (wishlist.Count != watchIds.Count
                || !wishlist.Select(w => w.Id).ToHashSet().SetEquals(watchIds))
            {
                throw new InvalidOperationException("Wishlist order must include every current wishlist watch.");
            }

            var priorities = watchIds
                .Select((watchId, priority) => (watchId, priority))
                .ToDictionary(item => item.watchId, item => item.priority);

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
}
