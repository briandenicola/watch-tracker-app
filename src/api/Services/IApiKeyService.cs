using WatchTracker.Api.DTOs;
using WatchTracker.Api.Models;

namespace WatchTracker.Api.Services;

public interface IApiKeyService
{
    Task<ApiKeyCreatedDto> CreateAsync(int userId, CreateApiKeyDto dto);
    Task<List<ApiKeyDto>> GetAllAsync(int userId);
    Task<bool> DeleteAsync(int id, int userId);
    Task<User?> ValidateAsync(string rawKey);
}
