using WatchTracker.Api.DTOs;

namespace WatchTracker.Api.Services;

public interface ISavedViewService
{
    Task<List<SavedViewDto>> GetAllAsync(int userId, CancellationToken ct = default);
    Task<SavedViewDto> CreateAsync(int userId, SaveSavedViewDto dto, CancellationToken ct = default);
    Task<SavedViewDto?> UpdateAsync(int id, int userId, SaveSavedViewDto dto, CancellationToken ct = default);
    Task<SavedViewDto?> SetPinnedAsync(int id, int userId, bool pinned, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, int userId, CancellationToken ct = default);
}
