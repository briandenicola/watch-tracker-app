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
    IWatchAnalysisService analysisService,
    DynamicConfigurationProvider dynamicConfig,
    ILogger<AdminController> logger) : ControllerBase
{
    [HttpGet("users")]
    [ProducesResponseType(typeof(List<UserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<UserDto>>> GetUsers()
    {
        var users = await adminService.ListUsersAsync();
        return Ok(users);
    }

    [HttpPost("users/{id}/unlock")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnlockUser(int id)
    {
        var result = await adminService.UnlockUserAsync(id);
        return result ? NoContent() : NotFound();
    }

    [HttpPost("users/{id}/reset-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResetPassword(int id, AdminResetPasswordDto dto)
    {
        var result = await adminService.ResetPasswordAsync(id, dto.NewPassword);
        return result ? NoContent() : NotFound();
    }

    [HttpGet("settings")]
    [ProducesResponseType(typeof(Dictionary<string, string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Dictionary<string, string>>> GetSettings()
    {
        var settings = await appSettings.GetAllAsync();
        return Ok(settings);
    }

    [HttpPut("settings")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateSettings(List<AppSettingDto> settings)
    {
        foreach (var s in settings)
        {
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

    [HttpPost("ollama/models")]
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<string>>> GetOllamaModels([FromBody] OllamaUrlDto dto, CancellationToken ct)
    {
        try
        {
            var models = await analysisService.GetOllamaModelsAsync(dto.Url, ct);
            return Ok(models);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
