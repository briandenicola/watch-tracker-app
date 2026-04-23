namespace WatchTracker.Api.Services;

public interface IBackgroundRemovalService : IDisposable
{
    Task<string> RemoveBackgroundAsync(string inputPath, CancellationToken cancellationToken = default);
    bool IsAvailable { get; }
}
