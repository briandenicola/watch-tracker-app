using WatchTracker.Api.DTOs;

namespace WatchTracker.Api.Services;

public interface IWatchService
{
    Task<IEnumerable<WatchDto>> GetAllAsync(int userId);
    Task<WatchDto?> GetByIdAsync(int id, int userId);
    Task<WatchDto> CreateAsync(CreateWatchDto dto, int userId);
    Task<WatchDto?> UpdateAsync(int id, UpdateWatchDto dto, int userId);
    Task<bool> DeleteAsync(int id, int userId);
    Task<WatchDto?> RecordWearAsync(int id, int userId);
    Task<IEnumerable<WearLogDto>> GetWearLogsAsync(int userId);
    Task<bool> DeleteWearLogAsync(int logId, int userId);
}
