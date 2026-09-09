using WatchTracker.Api.DTOs;

namespace WatchTracker.Api.Services;

public interface IAdminService
{
    Task<List<UserDto>> ListUsersAsync(int currentUserId);
    Task<bool> UnlockUserAsync(int userId);
    Task<bool> ResetPasswordAsync(int userId, string newPassword);
    Task<DeleteUserResult> DeleteUserAsync(int userId, int currentUserId);
}

public enum DeleteUserResult
{
    Deleted,
    NotFound,
    CannotDeleteSelf
}
