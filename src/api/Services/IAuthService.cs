using WatchTracker.Api.DTOs;

namespace WatchTracker.Api.Services;

public interface IAuthService
{
    Task<AuthResponseDto?> RegisterAsync(RegisterDto dto);
    Task<AuthResponseDto?> LoginAsync(LoginDto dto);
    Task<bool> ChangePasswordAsync(int userId, ChangePasswordDto dto);
    Task<AuthResponseDto?> GetProfileAsync(int userId);
    Task SetProfileImageAsync(int userId, string fileName);
    Task<string?> DeleteProfileImageAsync(int userId);
    Task<bool> UpdateUsernameAsync(int userId, string username);
}
