using WatchTracker.Api.DTOs;

namespace WatchTracker.Api.Services;

public interface IWatchWearLogService
{
    Task<WatchDto?> RecordWearAsync(int id, int userId, RecordWearDto? dto = null, CancellationToken ct = default);
    Task<IEnumerable<WearLogDto>> GetWearLogsAsync(int userId, CancellationToken ct = default);
    Task<bool> DeleteWearLogAsync(int logId, int userId, CancellationToken ct = default);
    Task<bool> UpdateWearLogAsync(int logId, int userId, UpdateWearLogDateDto dto, CancellationToken ct = default);
}
