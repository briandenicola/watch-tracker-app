using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WatchTracker.Api.DTOs;
using WatchTracker.Api.Services;

namespace WatchTracker.Api.Controllers;

/// <summary>
/// The one endpoint in the app that answers without an account. It takes a share
/// token and returns the redacted view of a single watch — nothing here accepts
/// a watch id, so a token is the only way in.
/// </summary>
[ApiController]
[Route("api/shared")]
[AllowAnonymous]
public class SharedWatchesController(IWatchShareService shares) : ControllerBase
{
    [HttpGet("{token}")]
    [EnableRateLimiting("public-share")]
    [ProducesResponseType(typeof(SharedWatchDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SharedWatchDto>> Get(string token, CancellationToken ct)
    {
        var watch = await shares.ViewAsync(token, ct);
        return watch is null ? NotFound() : Ok(watch);
    }
}
