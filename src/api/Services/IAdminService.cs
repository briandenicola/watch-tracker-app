using WatchTracker.Api.DTOs;

namespace WatchTracker.Api.Services;

public interface IAdminService
{
    Task<List<UserDto>> ListUsersAsync();
    Task<bool> UnlockUserAsync(int userId);
    Task<bool> ResetPasswordAsync(int userId, string newPassword);
}
