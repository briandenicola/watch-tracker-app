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
public class AdvisorController(ICollectionAdvisorService advisor) : ControllerBase
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
            return BadRequest(new { error = ex.Message });
        }
    }
}
