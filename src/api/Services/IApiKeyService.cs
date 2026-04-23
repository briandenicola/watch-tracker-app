using WatchTracker.Api.DTOs;
using WatchTracker.Api.Models;

namespace WatchTracker.Api.Services;

public interface IApiKeyService
{
    Task<ApiKeyCreatedDto> CreateAsync(int userId, CreateApiKeyDto dto, CancellationToken ct = default);
    Task<List<ApiKeyDto>> GetAllAsync(int userId, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, int userId, CancellationToken ct = default);
    Task<User?> ValidateAsync(string rawKey, CancellationToken ct = default);
}
