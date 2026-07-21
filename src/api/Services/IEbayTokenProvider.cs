namespace WatchTracker.Api.Services;

public interface IEbayTokenProvider
{
    Task<string?> GetAccessTokenAsync(CancellationToken ct = default);
}
