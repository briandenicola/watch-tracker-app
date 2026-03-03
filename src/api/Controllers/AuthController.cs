using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WatchTracker.Api.DTOs;
using WatchTracker.Api.Services;

namespace WatchTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto dto)
    {
        var result = await authService.RegisterAsync(dto);
        return result is null ? Conflict("Email already registered.") : Ok(result);
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginDto dto)
    {
        var result = await authService.LoginAsync(dto);
        return result is null ? Unauthorized("Invalid credentials.") : Ok(result);
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await authService.ChangePasswordAsync(userId, dto);
        return result ? NoContent() : BadRequest("Current password is incorrect.");
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<AuthResponseDto>> Me()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var profile = await authService.GetProfileAsync(userId);
        return profile is null ? NotFound() : Ok(profile);
    }

    [Authorize]
    [HttpPost("profile-image")]
    public async Task<ActionResult<object>> UploadProfileImage([FromForm] IFormFile file)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
        Directory.CreateDirectory(uploadsDir);

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var fileName = $"profile-{userId}{ext}";
        var filePath = Path.Combine(uploadsDir, fileName);

        await using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream);

        await authService.SetProfileImageAsync(userId, fileName);
        return Ok(new { profileImage = $"/uploads/{fileName}" });
    }

    [Authorize]
    [HttpDelete("profile-image")]
    public async Task<IActionResult> DeleteProfileImage()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var deleted = await authService.DeleteProfileImageAsync(userId);
        if (deleted is not null)
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "uploads", deleted);
            if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath);
        }
        return NoContent();
    }
}
