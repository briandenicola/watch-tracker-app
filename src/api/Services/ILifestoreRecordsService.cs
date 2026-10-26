using WatchTracker.Api.DTOs;

namespace WatchTracker.Api.Services;

public interface ILifestoreRecordsService
{
    /// <summary>
    /// One page of the user's watches and wish list items, oldest id first. An
    /// empty token starts at the beginning. Throws ArgumentException for a
    /// token this service did not hand out.
    /// </summary>
    Task<LifestoreRecordsPageDto> PageAsync(int userId, string? token, int limit, CancellationToken ct = default);
}
