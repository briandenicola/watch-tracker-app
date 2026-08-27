using WatchTracker.Api.DTOs;

namespace WatchTracker.Api.Services;

public interface IResaleValueService
{
    Task<WatchDto?> AddManualAsync(int watchId, int userId, CreateResaleValueEntryDto dto, CancellationToken ct = default);
    Task<IEnumerable<ResaleValueEntryDto>> GetHistoryAsync(int watchId, int userId, CancellationToken ct = default);
    Task<bool> DeleteEntryAsync(int entryId, int userId, CancellationToken ct = default);
}
