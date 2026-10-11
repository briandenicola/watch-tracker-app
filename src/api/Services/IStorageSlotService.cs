using WatchTracker.Api.DTOs;

namespace WatchTracker.Api.Services;

public interface IStorageSlotService
{
    Task UpdateSlotsAsync(int userId, UpdateStorageSlotsDto dto, CancellationToken ct = default);
}
