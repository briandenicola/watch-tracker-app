using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WatchTracker.Api.DTOs;
using WatchTracker.Api.Services;

namespace WatchTracker.Api.Controllers;

[ApiController]
[Route("api/wishlist/share/users")]
[Authorize]
public class WishlistUserSharesController(
    IWishlistShareService shares,
    ILogger<WishlistUserSharesController> logger) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("search")]
    [EnableRateLimiting("user-search")]
    public async Task<ActionResult<IReadOnlyList<WishlistShareUserDto>>> Search(
        [FromQuery, StringLength(100, MinimumLength = 2)] string query,
        CancellationToken ct) =>
        Ok(await shares.SearchUsersAsync(UserId, query, ct));

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<WishlistUserShareDto>>> Get(CancellationToken ct) =>
        Ok(await shares.GetUserSharesAsync(UserId, ct));

    [HttpPost]
    public async Task<ActionResult<WishlistUserShareDto>> Create(
        CreateWishlistUserShareDto request,
        CancellationToken ct)
    {
        var share = await shares.ShareWithUserAsync(UserId, request, ct);
        if (share is not null)
            logger.LogInformation(
                "Wish list access granted by user {OwnerUserId} to user {RecipientUserId}.",
                UserId,
                share.RecipientUserId);
        return share is null ? NotFound() : Ok(share);
    }

    [HttpDelete("{shareId:int}")]
    public async Task<IActionResult> Revoke(int shareId, CancellationToken ct)
    {
        var revoked = await shares.RevokeUserShareAsync(UserId, shareId, ct);
        if (revoked)
            logger.LogInformation(
                "Direct wish list share {ShareId} revoked by user {OwnerUserId}.",
                shareId,
                UserId);
        return revoked ? NoContent() : NotFound();
    }
}
