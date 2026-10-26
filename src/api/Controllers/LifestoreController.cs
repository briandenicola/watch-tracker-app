using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WatchTracker.Api.DTOs;
using WatchTracker.Api.Services;

namespace WatchTracker.Api.Controllers;

/// <summary>
/// The feed the household life store reads, with a read-only API key. It lists
/// every watch and wish list item the key's owner has, paged, and changes nothing.
/// </summary>
[ApiController]
[Route("api/lifestore")]
[Authorize]
public class LifestoreController(ILifestoreRecordsService recordsService) : ControllerBase
{
    private int UserId => int.Parse(
        User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("records")]
    [ProducesResponseType(typeof(LifestoreRecordsPageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LifestoreRecordsPageDto>> Records(
        [FromQuery] string? page = null,
        [FromQuery] int limit = LifestoreRecordsService.DefaultLimit,
        CancellationToken ct = default)
    {
        try
        {
            return Ok(await recordsService.PageAsync(UserId, page, limit, ct));
        }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
    }
}
