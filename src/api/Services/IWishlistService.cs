namespace WatchTracker.Api.Services;

public interface IWishlistService
{
    Task<bool> ReorderAsync(int userId, IReadOnlyList<int> watchIds, CancellationToken ct = default);
}
