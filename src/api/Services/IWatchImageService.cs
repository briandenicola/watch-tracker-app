using WatchTracker.Api.DTOs;

namespace WatchTracker.Api.Services;

public interface IWatchImageService
{
    Task<List<WatchImageDto>> UploadAsync(int watchId, int userId, IEnumerable<IFormFile> files);
    Task<WatchImageDto?> ImportFromUrlAsync(int watchId, int userId, string imageUrl);
    Task<bool> DeleteAsync(int imageId, int userId);
    Task<bool> SetCoverAsync(int watchId, int imageId, int userId);
    Task<WatchImageDto?> RemoveBackgroundAsync(int watchId, int imageId, int userId, CancellationToken cancellationToken = default);
}
