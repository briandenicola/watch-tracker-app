using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WatchTracker.Api.DTOs;
using WatchTracker.Api.Services;

namespace WatchTracker.Api.Controllers;

/// <summary>
/// The public read of a shared wish list. Like the single-watch equivalent it
/// answers without an account, and takes a token and nothing else — there is no
/// user id here for a visitor to guess at.
/// </summary>
[ApiController]
[Route("api/shared/wishlist")]
[AllowAnonymous]
public class SharedWishlistsController(IWishlistShareService shares) : ControllerBase
{
    [HttpGet("{token}")]
    [EnableRateLimiting("public-share")]
    [ProducesResponseType(typeof(SharedWishlistDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SharedWishlistDto>> Get(string token, CancellationToken ct)
    {
        var wishlist = await shares.ViewAsync(token, ct);
        return wishlist is null ? NotFound() : Ok(wishlist);
    }
}
