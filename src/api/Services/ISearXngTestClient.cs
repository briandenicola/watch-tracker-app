namespace WatchTracker.Api.Services;

public interface ISearXngTestClient
{
    Task<(bool Success, string Message)> TestConnectionAsync(string url, CancellationToken ct = default);
}
