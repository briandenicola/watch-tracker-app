using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WatchTracker.Api.DTOs;
using WatchTracker.Api.Services;

namespace WatchTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WatchesController(IWatchService watchService, IWatchAnalysisService analysisService) : ControllerBase
{
    private int UserId => int.Parse(
        User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<WatchDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<WatchDto>>> GetAll([FromQuery] bool includeRetired = false, CancellationToken ct = default)
    {
        var watches = await watchService.GetAllAsync(UserId, includeRetired, ct);
        return Ok(watches);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(WatchDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WatchDto>> GetById(int id, CancellationToken ct)
    {
        var watch = await watchService.GetByIdAsync(id, UserId, ct);
        return watch is null ? NotFound() : Ok(watch);
    }

    [HttpPost]
    [ProducesResponseType(typeof(WatchDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<WatchDto>> Create(CreateWatchDto dto, CancellationToken ct)
    {
        var watch = await watchService.CreateAsync(dto, UserId, ct);
        return CreatedAtAction(nameof(GetById), new { id = watch.Id }, watch);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(WatchDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WatchDto>> Update(int id, UpdateWatchDto dto, CancellationToken ct)
    {
        var watch = await watchService.UpdateAsync(id, dto, UserId, ct);
        return watch is null ? NotFound() : Ok(watch);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await watchService.DeleteAsync(id, UserId, ct);
        return result ? NoContent() : NotFound();
    }

    [HttpPost("{id}/analyze")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<object>> Analyze(int id, CancellationToken ct)
    {
        try
        {
            var analysis = await analysisService.AnalyzeAsync(id, UserId, ct);
            return Ok(new { analysis });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{id}/wear")]
    [ProducesResponseType(typeof(WatchDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WatchDto>> RecordWear(int id, CancellationToken ct)
    {
        var watch = await watchService.RecordWearAsync(id, UserId, ct);
        return watch is null ? NotFound() : Ok(watch);
    }

    [HttpGet("wear-logs")]
    [ProducesResponseType(typeof(IEnumerable<WearLogDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<WearLogDto>>> GetWearLogs(CancellationToken ct)
    {
        var logs = await watchService.GetWearLogsAsync(UserId, ct);
        return Ok(logs);
    }

    [HttpDelete("wear-logs/{logId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteWearLog(int logId, CancellationToken ct)
    {
        var deleted = await watchService.DeleteWearLogAsync(logId, UserId, ct);
        return deleted ? NoContent() : NotFound();
    }

    [HttpPut("wear-logs/{logId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateWearLogDate(int logId, [FromBody] UpdateWearLogDateDto dto, CancellationToken ct)
    {
        var updated = await watchService.UpdateWearLogDateAsync(logId, UserId, dto.WornDate, ct);
        return updated ? NoContent() : NotFound();
    }

    [HttpPut("{id}/retire")]
    [ProducesResponseType(typeof(WatchDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WatchDto>> Retire(int id, CancellationToken ct)
    {
        var watch = await watchService.RetireAsync(id, UserId, ct);
        return watch is null ? NotFound() : Ok(watch);
    }

    [HttpPut("{id}/unretire")]
    [ProducesResponseType(typeof(WatchDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WatchDto>> Unretire(int id, CancellationToken ct)
    {
        var watch = await watchService.UnretireAsync(id, UserId, ct);
        return watch is null ? NotFound() : Ok(watch);
    }
}
