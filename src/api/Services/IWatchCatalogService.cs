using WatchTracker.Api.DTOs;

namespace WatchTracker.Api.Services;

public interface IWatchCatalogService
{
    Task<IEnumerable<WatchDto>> GetAllAsync(int userId, bool includeDisposed = false, CancellationToken ct = default);
    Task<WatchDto?> GetByIdAsync(int id, int userId, CancellationToken ct = default);
    Task<WatchDto> CreateAsync(CreateWatchDto dto, int userId, CancellationToken ct = default);
    Task<WatchDto?> UpdateAsync(int id, UpdateWatchDto dto, int userId, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, int userId, CancellationToken ct = default);
}
