using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WatchTracker.Api.DTOs;
using WatchTracker.Api.Services;

namespace WatchTracker.Api.Controllers;

[ApiController]
[Route("api/collection/review")]
[Authorize]
public class CollectionReviewController(ICollectionReviewService review) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>The stored review, or 204 when one has never been generated.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(CollectionReviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult<CollectionReviewDto>> GetLatest(CancellationToken ct)
    {
        var latest = await review.GetLatestAsync(UserId, ct);
        return latest is null ? NoContent() : Ok(latest);
    }

    [HttpPost]
    [EnableRateLimiting("collection-review")]
    [ProducesResponseType(typeof(CollectionReviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CollectionReviewDto>> Generate(CancellationToken ct)
    {
        try
        {
            return Ok(await review.GenerateAsync(UserId, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
