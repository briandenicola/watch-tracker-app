using System.Collections.Concurrent;

namespace WatchTracker.Api.Services;

internal static class WishlistPriorityLocks
{
    private static readonly ConcurrentDictionary<int, SemaphoreSlim> Locks = new();

    public static SemaphoreSlim ForUser(int userId) => Locks.GetOrAdd(userId, _ => new SemaphoreSlim(1, 1));
}
