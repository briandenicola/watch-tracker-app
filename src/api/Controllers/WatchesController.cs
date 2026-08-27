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
    IWatchCatalogService watchCatalogService,
    IWatchWearLogService wearLogService,
    IWatchDispositionService dispositionService,
    IWishlistService wishlistService,
    IResaleValueService resaleValueService,
    IWatchAnalysisService analysisService,
    IWishlistExtractionService wishlistExtractionService,
    IResaleValueRefreshService resaleRefreshService,
    IWishlistPriceScanner priceScanner,
    IPriceAlertService priceAlertService,
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
        var watches = await watchCatalogService.GetAllAsync(UserId, includeDisposed || includeRetired, ct);
        return Ok(watches);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(WatchDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WatchDto>> GetById(int id, CancellationToken ct)
    {
        var watch = await watchCatalogService.GetByIdAsync(id, UserId, ct);
        return watch is null ? NotFound() : Ok(watch);
    }

    [HttpPost]
    [ProducesResponseType(typeof(WatchDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<WatchDto>> Create(CreateWatchDto dto, CancellationToken ct)
    {
        var watch = await watchCatalogService.CreateAsync(dto, UserId, ct);
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
        var watch = await watchCatalogService.UpdateAsync(id, dto, UserId, ct);
        return watch is null ? NotFound() : Ok(watch);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await watchCatalogService.DeleteAsync(id, UserId, ct);
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
            var watch = await wearLogService.RecordWearAsync(id, UserId, dto, ct);
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
        var logs = await wearLogService.GetWearLogsAsync(UserId, ct);
        return Ok(logs);
    }

    [HttpDelete("wear-logs/{logId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteWearLog(int logId, CancellationToken ct)
    {
        var deleted = await wearLogService.DeleteWearLogAsync(logId, UserId, ct);
        return deleted ? NoContent() : NotFound();
    }

    [HttpPut("wear-logs/{logId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateWearLogDate(int logId, [FromBody] UpdateWearLogDateDto dto, CancellationToken ct)
    {
        var updated = await wearLogService.UpdateWearLogAsync(logId, UserId, dto, ct);
        return updated ? NoContent() : NotFound();
    }

    [HttpGet("{id}/resale-history")]
    [ProducesResponseType(typeof(IEnumerable<ResaleValueEntryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ResaleValueEntryDto>>> GetResaleHistory(int id, CancellationToken ct)
    {
        var history = await resaleValueService.GetHistoryAsync(id, UserId, ct);
        return Ok(history);
    }

    [HttpPost("{id}/resale-value")]
    [ProducesResponseType(typeof(WatchDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WatchDto>> AddManualResaleValue(int id, CreateResaleValueEntryDto dto, CancellationToken ct)
    {
        var watch = await resaleValueService.AddManualAsync(id, UserId, dto, ct);
        return watch is null ? NotFound() : Ok(watch);
    }

    [HttpDelete("resale-history/{entryId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteResaleValueEntry(int entryId, CancellationToken ct)
    {
        var deleted = await resaleValueService.DeleteEntryAsync(entryId, UserId, ct);
        return deleted ? NoContent() : NotFound();
    }

    [HttpPost("{id}/resale-value/refresh")]
    [EnableRateLimiting("resale-refresh")]
    [ProducesResponseType(typeof(WatchDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WatchDto>> RefreshResaleValue(int id, CancellationToken ct)
    {
        var watch = await watchCatalogService.GetByIdAsync(id, UserId, ct);
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

    [HttpPut("{id}/price-monitoring")]
    [ProducesResponseType(typeof(PriceMonitoringDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PriceMonitoringDto>> UpdatePriceMonitoring(
        int id,
        UpdatePriceMonitoringDto dto,
        CancellationToken ct)
    {
        try
        {
            var monitoring = await priceScanner.UpdateMonitoringAsync(id, UserId, dto, ct);
            return monitoring is null ? NotFound() : Ok(monitoring);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{id}/price-scan")]
    [EnableRateLimiting("price-scan")]
    [ProducesResponseType(typeof(PriceScanResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<PriceScanResultDto>> ScanWishlistPrice(int id, CancellationToken ct)
    {
        try
        {
            var scan = await priceScanner.ScanAsync(id, UserId, ct);
            return scan is null ? NotFound() : Ok(scan);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{id}/price-observations")]
    [ProducesResponseType(typeof(IEnumerable<PriceObservationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<PriceObservationDto>>> GetPriceObservations(
        int id,
        CancellationToken ct)
    {
        var observations = await priceScanner.GetObservationsAsync(id, UserId, ct);
        return observations is null ? NotFound() : Ok(observations);
    }

    [HttpGet("price-alerts")]
    [ProducesResponseType(typeof(IEnumerable<PriceAlertDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<PriceAlertDto>>> GetPriceAlerts(
        [FromQuery] bool unreadOnly = false,
        CancellationToken ct = default)
    {
        return Ok(await priceAlertService.GetAlertsAsync(UserId, unreadOnly, ct));
    }

    [HttpPut("price-alerts/{alertId}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkPriceAlertRead(int alertId, CancellationToken ct)
    {
        var marked = await priceAlertService.MarkReadAsync(alertId, UserId, ct);
        return marked ? NoContent() : NotFound();
    }

    [HttpPut("price-alerts/read-all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkAllPriceAlertsRead(CancellationToken ct)
    {
        await priceAlertService.MarkAllReadAsync(UserId, ct);
        return NoContent();
    }

    [HttpPut("{id}/retire")]
    [ProducesResponseType(typeof(WatchDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WatchDto>> Retire(int id, CancellationToken ct)
    {
        try
        {
            var watch = await dispositionService.RetireAsync(id, UserId, ct);
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
        var watch = await dispositionService.UnretireAsync(id, UserId, ct);
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
            var watch = await dispositionService.SetDispositionAsync(id, UserId, dto, ct);
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
        var watch = await dispositionService.ClearDispositionAsync(id, UserId, ct);
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
            await wishlistService.ReorderAsync(UserId, dto.WatchIds, ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
