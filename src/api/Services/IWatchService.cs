using WatchTracker.Api.DTOs;

namespace WatchTracker.Api.Services;

public interface IWatchService
{
    Task<IEnumerable<WatchDto>> GetAllAsync(int userId, bool includeRetired = false, CancellationToken ct = default);
    Task<WatchDto?> GetByIdAsync(int id, int userId, CancellationToken ct = default);
    Task<WatchDto> CreateAsync(CreateWatchDto dto, int userId, CancellationToken ct = default);
    Task<WatchDto?> UpdateAsync(int id, UpdateWatchDto dto, int userId, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, int userId, CancellationToken ct = default);
    Task<WatchDto?> RecordWearAsync(int id, int userId, CancellationToken ct = default);
    Task<IEnumerable<WearLogDto>> GetWearLogsAsync(int userId, CancellationToken ct = default);
    Task<bool> DeleteWearLogAsync(int logId, int userId, CancellationToken ct = default);
    Task<bool> UpdateWearLogDateAsync(int logId, int userId, DateTime newDate, CancellationToken ct = default);
    Task<WatchDto?> RetireAsync(int id, int userId, CancellationToken ct = default);
    Task<WatchDto?> UnretireAsync(int id, int userId, CancellationToken ct = default);
}
