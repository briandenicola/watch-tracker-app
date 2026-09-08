using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WatchTracker.Api.DTOs;
using WatchTracker.Api.Services;

namespace WatchTracker.Api.Controllers;

[ApiController]
[Route("api/shared/wishlists")]
[Authorize]
public class ReceivedWishlistSharesController(IWishlistShareService shares) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ReceivedWishlistShareDto>>> Get(CancellationToken ct) =>
        Ok(await shares.GetReceivedSharesAsync(UserId, ct));

    [HttpGet("{shareId:int}")]
    public async Task<ActionResult<SharedWishlistDto>> View(int shareId, CancellationToken ct)
    {
        var share = await shares.ViewReceivedShareAsync(shareId, UserId, ct);
        return share is null ? NotFound() : Ok(share);
    }
}
