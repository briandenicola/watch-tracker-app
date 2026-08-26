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
public class WatchesController(
    IWatchService watchService,
    IWatchAnalysisService analysisService,
    IWishlistExtractionService wishlistExtractionService,
    IResaleValueRefreshService resaleRefreshService,
    IBackgroundTaskQueue taskQueue,
    ILogger<WatchesController> logger) : ControllerBase
{
    private int UserId => int.Parse(
        User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<WatchDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<WatchDto>>> GetAll(
        [FromQuery] bool includeDisposed = false,
        [FromQuery] bool includeRetired = false,
        CancellationToken ct = default)
    {
        var watches = await watchService.GetAllAsync(UserId, includeDisposed || includeRetired, ct);
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

    [HttpPost("wishlist/extract")]
    [EnableRateLimiting("wishlist-extraction")]
    [ProducesResponseType(typeof(WishlistExtractionResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<WishlistExtractionResultDto>> ExtractWishlist(
        WishlistExtractionRequestDto dto,
        CancellationToken ct)
    {
        try
        {
            return Ok(await wishlistExtractionService.ExtractAsync(dto.Url, ct));
        }
        catch (HttpRequestException)
        {
            return StatusCode(503, new { error = "The extraction service could not be reached." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
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
    [ProducesResponseType(typeof(WatchAnalysisResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<WatchAnalysisResultDto>> Analyze(int id, CancellationToken ct)
    {
        try
        {
            return Ok(await analysisService.AnalyzeAsync(id, UserId, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Writes the suggested values the owner approved. Nothing is written without this call.</summary>
    [HttpPost("{id}/analyze/apply")]
    [ProducesResponseType(typeof(ApplyAnalysisResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApplyAnalysisResultDto>> ApplyAnalysis(
        int id, ApplyAnalysisSuggestionsDto dto, CancellationToken ct)
    {
        var result = await analysisService.ApplySuggestionsAsync(id, UserId, dto, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{id}/wear")]
    [ProducesResponseType(typeof(WatchDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WatchDto>> RecordWear(
        int id,
        CancellationToken ct,
        [FromBody] RecordWearDto? dto = null)
    {
        try
        {
            var watch = await watchService.RecordWearAsync(id, UserId, dto, ct);
            return watch is null ? NotFound() : Ok(watch);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
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
        var updated = await watchService.UpdateWearLogAsync(logId, UserId, dto, ct);
        return updated ? NoContent() : NotFound();
    }

    [HttpGet("{id}/resale-history")]
    [ProducesResponseType(typeof(IEnumerable<ResaleValueEntryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ResaleValueEntryDto>>> GetResaleHistory(int id, CancellationToken ct)
    {
        var history = await watchService.GetResaleHistoryAsync(id, UserId, ct);
        return Ok(history);
    }

    [HttpPost("{id}/resale-value")]
    [ProducesResponseType(typeof(WatchDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WatchDto>> AddManualResaleValue(int id, CreateResaleValueEntryDto dto, CancellationToken ct)
    {
        var watch = await watchService.AddManualResaleValueAsync(id, UserId, dto, ct);
        return watch is null ? NotFound() : Ok(watch);
    }

    [HttpDelete("resale-history/{entryId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteResaleValueEntry(int entryId, CancellationToken ct)
    {
        var deleted = await watchService.DeleteResaleValueEntryAsync(entryId, UserId, ct);
        return deleted ? NoContent() : NotFound();
    }

    [HttpPost("{id}/resale-value/refresh")]
    [EnableRateLimiting("resale-refresh")]
    [ProducesResponseType(typeof(WatchDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WatchDto>> RefreshResaleValue(int id, CancellationToken ct)
    {
        var watch = await watchService.GetByIdAsync(id, UserId, ct);
        if (watch is null) return NotFound();

        var userId = UserId;
        taskQueue.QueueBackgroundWorkItem(async (services, workCt) =>
        {
            var refreshService = services.GetRequiredService<IResaleValueRefreshService>();
            try
            {
                await refreshService.RefreshWatchAsync(id, userId, workCt);
            }
            catch (InvalidOperationException ex)
            {
                logger.LogInformation("Queued resale value refresh skipped for watch {WatchId}: {Reason}", id, ex.Message);
            }
        });

        return Accepted(watch);
    }

    [HttpPut("{id}/retire")]
    [ProducesResponseType(typeof(WatchDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WatchDto>> Retire(int id, CancellationToken ct)
    {
        try
        {
            var watch = await watchService.RetireAsync(id, UserId, ct);
            return watch is null ? NotFound() : Ok(watch);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id}/unretire")]
    [ProducesResponseType(typeof(WatchDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WatchDto>> Unretire(int id, CancellationToken ct)
    {
        var watch = await watchService.UnretireAsync(id, UserId, ct);
        return watch is null ? NotFound() : Ok(watch);
    }

    [HttpPut("{id}/disposition")]
    [ProducesResponseType(typeof(WatchDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WatchDto>> SetDisposition(
        int id,
        UpdateWatchDispositionDto dto,
        CancellationToken ct)
    {
        try
        {
            var watch = await watchService.SetDispositionAsync(id, UserId, dto, ct);
            if (watch is not null)
                logger.LogInformation(
                    "Watch {WatchId} disposition set to {DispositionType} by user {UserId}",
                    id,
                    dto.Type,
                    UserId);
            return watch is null ? NotFound() : Ok(watch);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{id}/disposition")]
    [ProducesResponseType(typeof(WatchDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WatchDto>> ClearDisposition(int id, CancellationToken ct)
    {
        var watch = await watchService.ClearDispositionAsync(id, UserId, ct);
        if (watch is not null)
            logger.LogInformation("Watch {WatchId} disposition cleared by user {UserId}", id, UserId);
        return watch is null ? NotFound() : Ok(watch);
    }

    [HttpPut("wishlist/order")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ReorderWishlist(ReorderWishlistDto dto, CancellationToken ct)
    {
        try
        {
            await watchService.ReorderWishlistAsync(UserId, dto.WatchIds, ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
