using WatchTracker.Api.DTOs;

namespace WatchTracker.Api.Services;

public interface IWatchImageService
{
    Task<List<WatchImageDto>> UploadAsync(int watchId, int userId, IEnumerable<IFormFile> files, CancellationToken ct = default);
    Task<WatchImageDto?> ImportFromUrlAsync(int watchId, int userId, string imageUrl, CancellationToken ct = default);
    Task<bool> DeleteAsync(int imageId, int userId, CancellationToken ct = default);
    Task<bool> SetCoverAsync(int watchId, int imageId, int userId, CancellationToken ct = default);
    Task<WatchImageDto?> RemoveBackgroundAsync(int watchId, int imageId, int userId, CancellationToken cancellationToken = default);
}
