using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WatchTracker.Api.DTOs;
using WatchTracker.Api.Models;
using WatchTracker.Api.Services;

namespace WatchTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController(
    IAdminService adminService,
    IAppSettingsService appSettings,
    IOidcService oidcService,
    IWatchAnalysisService analysisService,
    IResaleValueRefreshService resaleRefreshService,
    IBackgroundTaskQueue taskQueue,
    ISearXngTestClient searXngTestClient,
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

        // Remove legacy settings that are no longer used
        settings.Remove("AnthropicApiKey");
        settings.Remove("AiProvider");

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

    [HttpGet("oidc/providers")]
    [ProducesResponseType(typeof(List<OidcProviderSettingsDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<OidcProviderSettingsDto>>> GetOidcProviders(CancellationToken ct)
    {
        return Ok(await oidcService.GetAdminProvidersAsync(ct));
    }

    [HttpPut("oidc/providers/{provider}")]
    [ProducesResponseType(typeof(OidcProviderSettingsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<OidcProviderSettingsDto>> UpdateOidcProvider(
        OidcProvider provider,
        UpdateOidcProviderSettingsDto dto,
        CancellationToken ct)
    {
        var result = await oidcService.UpdateProviderAsync(provider, dto, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut("oidc/providers/{provider}/secret")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetOidcProviderSecret(
        OidcProvider provider,
        UpdateOidcProviderSecretDto dto,
        CancellationToken ct)
    {
        await oidcService.SetClientSecretAsync(provider, dto.ClientSecret, ct);
        return NoContent();
    }

    [HttpPost("oidc/providers/{provider}/test")]
    [ProducesResponseType(typeof(OidcProviderTestResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<OidcProviderTestResultDto>> TestOidcProvider(
        OidcProvider provider,
        CancellationToken ct)
    {
        return Ok(await oidcService.TestProviderAsync(provider, ct));
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

    [HttpPost("searxng/test")]
    [ProducesResponseType(typeof(ConnectionTestResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ConnectionTestResultDto>> TestSearXng([FromBody] SearXngUrlDto dto, CancellationToken ct)
    {
        var (success, message) = await searXngTestClient.TestConnectionAsync(dto.Url, ct);
        return Ok(new ConnectionTestResultDto { Success = success, Message = message });
    }

    [HttpPost("resale-values/refresh-all")]
    [EnableRateLimiting("resale-refresh")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public IActionResult RefreshAllResaleValues()
    {
        taskQueue.QueueBackgroundWorkItem(async (services, workCt) =>
        {
            var refreshService = services.GetRequiredService<IResaleValueRefreshService>();
            var summary = await refreshService.RefreshAllNowAsync(workCt);
            logger.LogInformation(
                "Resale value refresh (admin-triggered) finished: {Refreshed}/{Due} refreshed, {Skipped} skipped, {Failed} failed.",
                summary.Refreshed, summary.Due, summary.Skipped, summary.Failed);
        });

        return Accepted(new { message = "Resale value refresh queued for all watches. Check individual watches or the server logs for results." });
    }
}
