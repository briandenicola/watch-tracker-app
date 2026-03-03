using WatchTracker.Api.Models;

namespace WatchTracker.Api.DTOs;

public class UserDto
{
    public int Id { get; set; }
    public required string Username { get; set; }
    public required string Email { get; set; }
    public UserRole Role { get; set; }
    public bool IsLockedOut { get; set; }
    public int FailedLoginAttempts { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AdminResetPasswordDto
{
    public required string NewPassword { get; set; }
}

public class AppSettingDto
{
    public required string Key { get; set; }
    public required string Value { get; set; }
}
