using WatchTracker.Api.DTOs;

namespace WatchTracker.Api.Services;

public interface IAuthService
{
    Task<AuthResponseDto?> RegisterAsync(RegisterDto dto);
    Task<AuthResponseDto?> LoginAsync(LoginDto dto);
    Task<bool> ChangePasswordAsync(int userId, ChangePasswordDto dto);
}
