using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WatchTracker.Api.DTOs;
using WatchTracker.Api.Services;

namespace WatchTracker.Api.Controllers;

[ApiController]
[Route("api/advisor")]
[Authorize]
public class AdvisorController(
    ICollectionAdvisorService advisor,
    ILogger<AdvisorController> logger) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    [ProducesResponseType(typeof(AdvisorChatStateDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AdvisorChatStateDto>> GetCurrent(CancellationToken ct) =>
        Ok(await advisor.GetCurrentStateAsync(UserId, ct));

    [HttpGet("sessions/{sessionId:int}")]
    [ProducesResponseType(typeof(AdvisorChatStateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdvisorChatStateDto>> GetSession(
        int sessionId,
        CancellationToken ct)
    {
        var state = await advisor.GetStateAsync(sessionId, UserId, ct);
        return state is null ? NotFound() : Ok(state);
    }

    [HttpPost("sessions")]
    [ProducesResponseType(typeof(AdvisorChatStateDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<AdvisorChatStateDto>> StartSession(CancellationToken ct)
    {
        var state = await advisor.StartNewSessionAsync(UserId, ct);
        return CreatedAtAction(nameof(GetSession), new { sessionId = state.Session.Id }, state);
    }

    [HttpPost("sessions/{sessionId:int}/messages")]
    [EnableRateLimiting("collection-advisor")]
    [ProducesResponseType(typeof(AdvisorChatStateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdvisorChatStateDto>> SendMessage(
        int sessionId,
        SendAdvisorMessageDto dto,
        CancellationToken ct)
    {
        try
        {
            var state = await advisor.SendMessageAsync(sessionId, UserId, dto, ct);
            return state is null ? NotFound() : Ok(state);
        }
        catch (InvalidOperationException ex)
        {
            // A 400 carries its reason to the user but used to leave no trace here,
            // so a failing advisor looked identical to a healthy one in the log.
            logger.LogWarning("An advisor message was rejected.");
            logger.LogDebug(
                ex,
                "Advisor message in session {SessionId} for user {UserId} was rejected: {Reason}",
                sessionId,
                UserId,
                ex.Message);
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("messages/{messageId:int}/feedback")]
    [ProducesResponseType(typeof(AdvisorRecommendationFeedbackDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdvisorRecommendationFeedbackDto>> SaveFeedback(
        int messageId,
        SaveAdvisorFeedbackDto dto,
        CancellationToken ct)
    {
        var feedback = await advisor.SaveFeedbackAsync(messageId, UserId, dto, ct);
        return feedback is null ? NotFound() : Ok(feedback);
    }

    [HttpDelete("feedback/{feedbackId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveFeedback(int feedbackId, CancellationToken ct) =>
        await advisor.RemoveFeedbackAsync(feedbackId, UserId, ct) ? NoContent() : NotFound();

    [HttpPost("messages/{messageId:int}/wishlist")]
    [ProducesResponseType(typeof(AdvisorWishlistActionResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AdvisorWishlistActionResultDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdvisorWishlistActionResultDto>> AddToWishlist(
        int messageId,
        AdvisorRecommendationActionDto dto,
        CancellationToken ct)
    {
        var result = await advisor.AddToWishlistAsync(messageId, UserId, dto, ct);
        if (result is null) return NotFound();
        return result.Added
            ? Created($"/api/watches/{result.WatchId}", result)
            : Ok(result);
    }
}
