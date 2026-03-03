using WatchTracker.Api.DTOs;

namespace WatchTracker.Api.Services;

public interface IWatchImageService
{
    Task<List<WatchImageDto>> UploadAsync(int watchId, int userId, IEnumerable<IFormFile> files);
    Task<bool> DeleteAsync(int imageId, int userId);
}
