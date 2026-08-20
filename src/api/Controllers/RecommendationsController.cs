using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WatchTracker.Api.DTOs;
using WatchTracker.Api.Services;

namespace WatchTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RecommendationsController(IWatchRecommendationService recommendationService) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost("outfit")]
    [EnableRateLimiting("watch-recommendation")]
    [ProducesResponseType(typeof(WatchRecommendationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<WatchRecommendationDto>> RecommendForOutfit(
        WatchRecommendationRequestDto request,
        CancellationToken ct)
    {
        try
        {
            return Ok(await recommendationService.RecommendAsync(request, UserId, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
