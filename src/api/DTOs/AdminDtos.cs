using System.ComponentModel.DataAnnotations;
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
    [Required, StringLength(128, MinimumLength = 8)]
    public required string NewPassword { get; set; }
}

public class AppSettingDto
{
    [Required, StringLength(200)]
    public required string Key { get; set; }

    [Required, StringLength(10000)]
    public required string Value { get; set; }
}

public class OllamaUrlDto
{
    [Required, StringLength(2000), Url]
    public required string Url { get; set; }
}
