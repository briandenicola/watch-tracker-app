using System.ComponentModel.DataAnnotations;

namespace WatchTracker.Api.DTOs;

public class RegisterDto
{
    [Required, StringLength(100, MinimumLength = 2)]
    public required string Username { get; set; }

    [Required, StringLength(254), EmailAddress]
    public required string Email { get; set; }

    [Required, StringLength(128, MinimumLength = 8)]
    public required string Password { get; set; }
}

public class LoginDto
{
    [Required, StringLength(254), EmailAddress]
    public required string Email { get; set; }

    [Required, StringLength(128)]
    public required string Password { get; set; }
}

public class AuthResponseDto
{
    public required string Token { get; set; }
    public string? RefreshToken { get; set; }
    public required string Username { get; set; }
    public required string Email { get; set; }
    public required string Role { get; set; }
    public string? ProfileImage { get; set; }
}

public class RefreshTokenRequestDto
{
    [Required]
    public required string RefreshToken { get; set; }
}

public class ChangePasswordDto
{
    [Required, StringLength(128)]
    public required string CurrentPassword { get; set; }

    [Required, StringLength(128, MinimumLength = 8)]
    public required string NewPassword { get; set; }
}

public class UpdateUsernameDto
{
    [Required, StringLength(100, MinimumLength = 2)]
    public required string Username { get; set; }
}
