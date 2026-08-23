using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WatchTracker.Api.DTOs;
using WatchTracker.Api.Services;

namespace WatchTracker.Api.Controllers;

/// <summary>The owner's controls for a watch's public share link.</summary>
[ApiController]
[Route("api/watches/{watchId:int}/share")]
[Authorize]
public class WatchShareController(IWatchShareService shares, ILogger<WatchShareController> logger) : ControllerBase
{
    private int UserId => int.Parse(
        User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>The link for this watch. 404 means it has never been shared.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(WatchShareDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WatchShareDto>> Get(int watchId, CancellationToken ct)
    {
        var share = await shares.GetAsync(watchId, UserId, ct);
        return share is null ? NotFound() : Ok(share);
    }

    [HttpPost]
    [ProducesResponseType(typeof(WatchShareDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WatchShareDto>> Create(int watchId, CancellationToken ct)
    {
        var share = await shares.CreateAsync(watchId, UserId, ct);
        if (share is null) return NotFound();

        logger.LogInformation("Watch {WatchId} share link issued by user {UserId}", watchId, UserId);
        return Ok(share);
    }

    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Revoke(int watchId, CancellationToken ct)
    {
        var revoked = await shares.RevokeAsync(watchId, UserId, ct);
        if (revoked) logger.LogInformation("Watch {WatchId} share link revoked by user {UserId}", watchId, UserId);
        return revoked ? NoContent() : NotFound();
    }
}
