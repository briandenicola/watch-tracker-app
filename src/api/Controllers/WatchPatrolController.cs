using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WatchTracker.Api.DTOs;
using WatchTracker.Api.Services;

namespace WatchTracker.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/advisor/watchpatrol")]
public class WatchPatrolController(
    IWatchPatrolConnectionService connections,
    IConfiguration configuration,
    ILogger<WatchPatrolController> logger) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<ActionResult<WatchPatrolStatusDto>> GetStatus(CancellationToken ct) =>
        Ok(await connections.GetStatusAsync(UserId, ct));

    [HttpPost("connect")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<ActionResult<WatchPatrolAuthorizationDto>> Connect(ConnectWatchPatrolDto dto, CancellationToken ct) =>
        RunAsync(async () =>
        {
            if (!IsAllowedRedirect(dto.RedirectUri))
                throw new InvalidOperationException("The WatchPatrol redirect must be this app's Advisor page.");
            return new WatchPatrolAuthorizationDto(await connections.ConnectAsync(UserId, dto.RedirectUri, ct));
        });

    [HttpPost("complete")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<ActionResult<WatchPatrolStatusDto>> Complete(CompleteWatchPatrolDto dto, CancellationToken ct) =>
        RunAsync(async () =>
        {
            await connections.CompleteAsync(UserId, dto.Code, dto.State, ct);
            return new WatchPatrolStatusDto(true);
        });

    [HttpDelete]
    public async Task<IActionResult> Disconnect(CancellationToken ct)
    {
        await connections.DisconnectAsync(UserId, ct);
        return NoContent();
    }

    private bool IsAllowedRedirect(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)
            || (uri.Scheme != "https" && !(uri.Scheme == "http" && uri.IsLoopback))
            || uri.UserInfo.Length > 0 || uri.Query.Length > 0 || uri.Fragment.Length > 0
            || uri.AbsolutePath != "/advisor")
            return false;
        var origin = uri.GetLeftPart(UriPartial.Authority);
        var sameOrigin = $"{Request.Scheme}://{Request.Host}";
        var allowed = configuration["AllowedOrigins"] ?? "http://localhost:5173";
        return origin.Equals(sameOrigin, StringComparison.OrdinalIgnoreCase)
            || allowed.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Any(candidate => candidate.Equals(origin, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<ActionResult<T>> RunAsync<T>(Func<Task<T>> action)
    {
        try
        {
            return Ok(await action());
        }
        catch (Exception ex) when (ex is InvalidOperationException or HttpRequestException
            or TaskCanceledException && !HttpContext.RequestAborted.IsCancellationRequested)
        {
            logger.LogWarning("WatchPatrol connection operation failed ({ErrorType}).", ex.GetType().Name);
            return BadRequest(new
            {
                error = ex is InvalidOperationException ? ex.Message
                    : "WatchPatrol is unreachable. Try connecting again."
            });
        }
    }
}
