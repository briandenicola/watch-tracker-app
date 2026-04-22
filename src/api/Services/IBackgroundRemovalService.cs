namespace WatchTracker.Api.Services;

public interface IBackgroundRemovalService
{
    Task<string> RemoveBackgroundAsync(string inputPath, CancellationToken cancellationToken = default);
    bool IsAvailable { get; }
}
