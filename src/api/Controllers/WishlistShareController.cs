using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WatchTracker.Api.DTOs;
using WatchTracker.Api.Services;

namespace WatchTracker.Api.Controllers;

/// <summary>The owner's controls for the public link to their wish list.</summary>
[ApiController]
[Route("api/wishlist/share")]
[Authorize]
public class WishlistShareController(
    IWishlistShareService shares,
    ILogger<WishlistShareController> logger) : ControllerBase
{
    private int UserId => int.Parse(
        User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>The link for this user's wish list. 404 means it has never been shared.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(WishlistShareDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WishlistShareDto>> Get(CancellationToken ct)
    {
        var share = await shares.GetAsync(UserId, ct);
        return share is null ? NotFound() : Ok(share);
    }

    [HttpPost]
    [ProducesResponseType(typeof(WishlistShareDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<WishlistShareDto>> Create(UpdateWishlistShareDto dto, CancellationToken ct)
    {
        var share = await shares.CreateAsync(UserId, dto, ct);
        logger.LogInformation("Wish list share link issued for user {UserId}", UserId);
        return Ok(share);
    }

    /// <summary>Changes what the link exposes. The link itself is unchanged.</summary>
    [HttpPut]
    [ProducesResponseType(typeof(WishlistShareDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WishlistShareDto>> Update(UpdateWishlistShareDto dto, CancellationToken ct)
    {
        var share = await shares.UpdateAsync(UserId, dto, ct);
        return share is null ? NotFound() : Ok(share);
    }

    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Revoke(CancellationToken ct)
    {
        var revoked = await shares.RevokeAsync(UserId, ct);
        if (revoked) logger.LogInformation("Wish list share link revoked for user {UserId}", UserId);
        return revoked ? NoContent() : NotFound();
    }
}
