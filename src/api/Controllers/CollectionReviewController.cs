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

    /// <summary>
    /// Whether a review can be generated, and the stored one when there is one.
    /// A user who has never run one still needs the configured flag, so this
    /// answers with state rather than nothing.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(CollectionReviewStateDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CollectionReviewStateDto>> GetLatest(CancellationToken ct) =>
        Ok(await review.GetStateAsync(UserId, ct));

    [HttpPost]
    [EnableRateLimiting("collection-review")]
    [ProducesResponseType(typeof(CollectionReviewStateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CollectionReviewStateDto>> Generate(CancellationToken ct)
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
