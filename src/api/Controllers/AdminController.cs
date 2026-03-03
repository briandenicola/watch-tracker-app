using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WatchTracker.Api.DTOs;
using WatchTracker.Api.Services;

namespace WatchTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController(IAdminService adminService, IAppSettingsService appSettings) : ControllerBase
{
    [HttpGet("users")]
    public async Task<ActionResult<List<UserDto>>> GetUsers()
    {
        var users = await adminService.ListUsersAsync();
        return Ok(users);
    }

    [HttpPost("users/{id}/unlock")]
    public async Task<IActionResult> UnlockUser(int id)
    {
        var result = await adminService.UnlockUserAsync(id);
        return result ? NoContent() : NotFound();
    }

    [HttpPost("users/{id}/reset-password")]
    public async Task<IActionResult> ResetPassword(int id, AdminResetPasswordDto dto)
    {
        var result = await adminService.ResetPasswordAsync(id, dto.NewPassword);
        return result ? NoContent() : NotFound();
    }

    [HttpGet("settings")]
    public async Task<ActionResult<Dictionary<string, string>>> GetSettings()
    {
        var settings = await appSettings.GetAllAsync();
        return Ok(settings);
    }

    [HttpPut("settings")]
    public async Task<IActionResult> UpdateSettings(List<AppSettingDto> settings)
    {
        foreach (var s in settings)
        {
            await appSettings.SetAsync(s.Key, s.Value);
        }
        return NoContent();
    }
}
