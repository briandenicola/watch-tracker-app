using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WatchTracker.Api.DTOs;
using WatchTracker.Api.Models;
using WatchTracker.Api.Services;

namespace WatchTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IAuthService authService, IOidcService oidcService) : ControllerBase
{
    [HttpPost("register")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto dto)
    {
        var result = await authService.RegisterAsync(dto);
        return result is null ? Conflict(new { error = "Email already registered." }) : Ok(result);
    }

    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginDto dto)
    {
        var result = await authService.LoginAsync(dto);
        return result is null ? Unauthorized(new { error = "Invalid credentials." }) : Ok(result);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponseDto>> Refresh(RefreshTokenRequestDto dto)
    {
        var result = await authService.RefreshAsync(dto.RefreshToken);
        return result is null ? Unauthorized(new { error = "Invalid or expired refresh token." }) : Ok(result);
    }

    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(RefreshTokenRequestDto dto)
    {
        await authService.RevokeRefreshTokenAsync(dto.RefreshToken);
        return NoContent();
    }

    [Authorize]
    [HttpPost("change-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await authService.ChangePasswordAsync(userId, dto);
        return result ? NoContent() : BadRequest(new { error = "Current password is incorrect." });
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AuthResponseDto>> Me()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var profile = await authService.GetProfileAsync(userId);
        return profile is null ? NotFound() : Ok(profile);
    }

    [Authorize]
    [HttpPut("username")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUsername(UpdateUsernameDto dto)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await authService.UpdateUsernameAsync(userId, dto.Username);
        return result ? NoContent() : NotFound();
    }

    [Authorize]
    [HttpPost("profile-image")]
    [ProducesResponseType(StatusCodes.Status200OK)]
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
    [ProducesResponseType(StatusCodes.Status204NoContent)]
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

    [HttpGet("oidc/providers")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<OidcProviderPublicDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<OidcProviderPublicDto>>> GetOidcProviders(CancellationToken ct)
    {
        return Ok(await oidcService.GetEnabledProvidersAsync(ct));
    }

    [HttpGet("oidc/{provider}/login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> StartOidcLogin(
        OidcProvider provider,
        [FromQuery] string? returnUrl,
        CancellationToken ct)
    {
        var url = await oidcService.BuildLoginUrlAsync(provider, Request, returnUrl, null, ct);
        return url is null ? BadRequest(new { error = "OIDC provider is not enabled or configured." }) : Redirect(url);
    }

    [HttpGet("oidc/{provider}/link")]
    [Authorize]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> StartOidcLink(
        OidcProvider provider,
        [FromQuery] string? returnUrl,
        CancellationToken ct)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var url = await oidcService.BuildLoginUrlAsync(provider, Request, returnUrl ?? "/settings", userId, ct);
        return url is null ? BadRequest(new { error = "OIDC provider is not enabled or configured." }) : Redirect(url);
    }

    [HttpPost("oidc/{provider}/link-url")]
    [Authorize]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<object>> CreateOidcLinkUrl(
        OidcProvider provider,
        [FromQuery] string? returnUrl,
        CancellationToken ct)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var url = await oidcService.BuildLoginUrlAsync(provider, Request, returnUrl ?? "/settings", userId, ct);
        return url is null ? BadRequest(new { error = "OIDC provider is not enabled or configured." }) : Ok(new { url });
    }

    [HttpGet("oidc/{provider}/complete")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status302Found)]
    public async Task<IActionResult> CompleteOidcLogin(
        OidcProvider provider,
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? error,
        CancellationToken ct)
    {
        var redirect = await oidcService.CompleteLoginAsync(provider, Request, code, state, error, ct);
        return Redirect(redirect);
    }

    [HttpPost("oidc/exchange")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponseDto>> ExchangeOidcCode(OidcExchangeDto dto, CancellationToken ct)
    {
        var result = await oidcService.ExchangeCodeAsync(dto.Code, ct);
        return result is null ? Unauthorized(new { error = "Invalid or expired OIDC login code." }) : Ok(result);
    }

    [Authorize]
    [HttpGet("oidc/linked")]
    [ProducesResponseType(typeof(List<LinkedOidcProviderDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<LinkedOidcProviderDto>>> GetLinkedOidcProviders(CancellationToken ct)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(await oidcService.GetLinkedProvidersAsync(userId, ct));
    }

    [Authorize]
    [HttpDelete("oidc/{provider}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UnlinkOidcProvider(OidcProvider provider, CancellationToken ct)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await oidcService.UnlinkProviderAsync(userId, provider, ct);
        return NoContent();
    }
}
