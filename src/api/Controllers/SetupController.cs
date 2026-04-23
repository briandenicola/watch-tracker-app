using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using WatchTracker.Api.Data;
using WatchTracker.Api.DTOs;
using WatchTracker.Api.Models;
using WatchTracker.Api.Services;

namespace WatchTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("auth")]
public class SetupController(AppDbContext context, IAuthService authService, IAppSettingsService appSettings) : ControllerBase
{
    [HttpGet("status")]
    public async Task<ActionResult<object>> GetStatus()
    {
        var needsSetup = !await context.Users.AnyAsync();
        return Ok(new { needsSetup });
    }

    [HttpPost]
    public async Task<ActionResult<AuthResponseDto>> Setup(SetupDto dto)
    {
        if (await context.Users.AnyAsync())
            return Conflict("Setup has already been completed.");

        // Create admin user
        var user = new User
        {
            Username = dto.Username,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = UserRole.Admin,
            CreatedAt = DateTime.UtcNow
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Save optional settings
        if (!string.IsNullOrWhiteSpace(dto.AiProvider))
            await appSettings.SetAsync(AppSettingsService.Keys.AiProvider, dto.AiProvider);

        if (!string.IsNullOrWhiteSpace(dto.AnthropicApiKey))
            await appSettings.SetAsync("AnthropicApiKey", dto.AnthropicApiKey);

        if (!string.IsNullOrWhiteSpace(dto.OllamaUrl))
            await appSettings.SetAsync(AppSettingsService.Keys.OllamaUrl, dto.OllamaUrl);

        if (!string.IsNullOrWhiteSpace(dto.OllamaModel))
            await appSettings.SetAsync(AppSettingsService.Keys.OllamaModel, dto.OllamaModel);

        if (!string.IsNullOrWhiteSpace(dto.AiAnalysisPrompt))
            await appSettings.SetAsync(AppSettingsService.Keys.AiAnalysisPrompt, dto.AiAnalysisPrompt);

        // Return auth token so the user is logged in immediately
        var loginResult = await authService.LoginAsync(new LoginDto
        {
            Email = dto.Email,
            Password = dto.Password
        });

        return Ok(loginResult);
    }
}
