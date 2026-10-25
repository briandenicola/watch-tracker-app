using WatchTracker.Api.DTOs;

namespace WatchTracker.Api.Services;

public interface ILabelService
{
    Task<List<LabelDto>> GetAllAsync(int userId, CancellationToken ct = default);
    Task<LabelDto> CreateAsync(int userId, SaveLabelDto dto, CancellationToken ct = default);
    Task<LabelDto?> UpdateAsync(int id, int userId, SaveLabelDto dto, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, int userId, CancellationToken ct = default);
    Task<List<int>?> SetWatchLabelsAsync(int watchId, int userId, IReadOnlyCollection<int> labelIds, CancellationToken ct = default);
}
