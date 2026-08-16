using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WatchTracker.Api.DTOs;
using WatchTracker.Api.Services;

namespace WatchTracker.Api.Controllers;

/// <summary>The style agent chat for a single watch.</summary>
[ApiController]
[Route("api/watches/{watchId:int}/style")]
[Authorize]
public class StyleController(IStyleAgentService styleAgent) : ControllerBase
{
    private int UserId => int.Parse(
        User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    [ProducesResponseType(typeof(StyleChatStateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StyleChatStateDto>> GetState(int watchId, CancellationToken ct)
    {
        var state = await styleAgent.GetStateAsync(watchId, UserId, ct);
        return state is null ? NotFound() : Ok(state);
    }

    [HttpPost("messages")]
    [EnableRateLimiting("style-agent")]
    [ProducesResponseType(typeof(StyleChatStateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StyleChatStateDto>> SendMessage(
        int watchId, SendStyleMessageDto dto, CancellationToken ct)
    {
        try
        {
            var state = await styleAgent.SendMessageAsync(watchId, UserId, dto, ct);
            return state is null ? NotFound() : Ok(state);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("sessions")]
    [ProducesResponseType(typeof(StyleChatStateDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StyleChatStateDto>> StartNewSession(int watchId, CancellationToken ct)
    {
        var state = await styleAgent.StartNewSessionAsync(watchId, UserId, ct);
        return state is null
            ? NotFound()
            : CreatedAtAction(nameof(GetState), new { watchId }, state);
    }

    [HttpPost("recommendations/{recommendationId:int}/feedback")]
    [ProducesResponseType(typeof(StyleRecommendationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StyleRecommendationDto>> RecordFeedback(
        int watchId, int recommendationId, StyleFeedbackDto dto, CancellationToken ct)
    {
        var recommendation = await styleAgent.RecordFeedbackAsync(watchId, recommendationId, UserId, dto, ct);
        return recommendation is null ? NotFound() : Ok(recommendation);
    }

    [HttpDelete("recommendations/{recommendationId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ForgetRecommendation(int watchId, int recommendationId, CancellationToken ct)
    {
        var forgotten = await styleAgent.ForgetRecommendationAsync(watchId, recommendationId, UserId, ct);
        return forgotten ? NoContent() : NotFound();
    }
}
