using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WatchTracker.Api.DTOs;
using WatchTracker.Api.Services;

namespace WatchTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController(
    IAdminService adminService,
    IAppSettingsService appSettings,
    DynamicConfigurationProvider dynamicConfig,
    ILogger<AdminController> logger) : ControllerBase
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

        // Mask the Anthropic API key – only expose the first 10 characters
        if (settings.TryGetValue("AnthropicApiKey", out var apiKey) && apiKey.Length > 10)
        {
            settings["AnthropicApiKey"] = apiKey[..10] + "...";
        }

        return Ok(settings);
    }

    [HttpPut("settings")]
    public async Task<IActionResult> UpdateSettings(List<AppSettingDto> settings)
    {
        foreach (var s in settings)
        {
            // Skip masked API key values to avoid overwriting with truncated data
            if (s.Key == "AnthropicApiKey" && s.Value.EndsWith("..."))
                continue;

            await appSettings.SetAsync(s.Key, s.Value);
        }

        var logLevelEntry = settings.FirstOrDefault(s => s.Key == AppSettingsService.Keys.LogLevel);
        if (logLevelEntry is not null)
        {
            dynamicConfig.Set("Logging:LogLevel:Default", logLevelEntry.Value);
            logger.LogInformation("Log level changed to {LogLevel}", logLevelEntry.Value);
        }

        return NoContent();
    }
}
