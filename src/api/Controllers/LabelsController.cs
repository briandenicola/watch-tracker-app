using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WatchTracker.Api.DTOs;
using WatchTracker.Api.Services;

namespace WatchTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LabelsController(ILabelService labelService) : ControllerBase
{
    private int UserId => int.Parse(
        User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    [ProducesResponseType(typeof(List<LabelDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<LabelDto>>> GetAll(CancellationToken ct) =>
        Ok(await labelService.GetAllAsync(UserId, ct));

    [HttpPost]
    [ProducesResponseType(typeof(LabelDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LabelDto>> Create(SaveLabelDto dto, CancellationToken ct)
    {
        try
        {
            var label = await labelService.CreateAsync(UserId, dto, ct);
            return CreatedAtAction(nameof(GetAll), label);
        }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(LabelDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LabelDto>> Update(int id, SaveLabelDto dto, CancellationToken ct)
    {
        try
        {
            var label = await labelService.UpdateAsync(id, UserId, dto, ct);
            return label is null ? NotFound() : Ok(label);
        }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct) =>
        await labelService.DeleteAsync(id, UserId, ct) ? NoContent() : NotFound();

    [HttpPut("~/api/watches/{watchId}/labels")]
    [ProducesResponseType(typeof(List<int>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<int>>> SetWatchLabels(int watchId, SetWatchLabelsDto dto, CancellationToken ct)
    {
        try
        {
            var labelIds = await labelService.SetWatchLabelsAsync(watchId, UserId, dto.LabelIds, ct);
            return labelIds is null ? NotFound() : Ok(labelIds);
        }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
    }
}
