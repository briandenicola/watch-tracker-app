using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WatchTracker.Api.DTOs;
using WatchTracker.Api.Services;

namespace WatchTracker.Api.Controllers;

[ApiController]
[Route("api/saved-views")]
[Authorize]
public class SavedViewsController(ISavedViewService savedViewService) : ControllerBase
{
    private int UserId => int.Parse(
        User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    [ProducesResponseType(typeof(List<SavedViewDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<SavedViewDto>>> GetAll(CancellationToken ct) =>
        Ok(await savedViewService.GetAllAsync(UserId, ct));

    [HttpPost]
    [ProducesResponseType(typeof(SavedViewDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SavedViewDto>> Create(SaveSavedViewDto dto, CancellationToken ct)
    {
        try
        {
            var view = await savedViewService.CreateAsync(UserId, dto, ct);
            return CreatedAtAction(nameof(GetAll), view);
        }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(SavedViewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SavedViewDto>> Update(int id, SaveSavedViewDto dto, CancellationToken ct)
    {
        try
        {
            var view = await savedViewService.UpdateAsync(id, UserId, dto, ct);
            return view is null ? NotFound() : Ok(view);
        }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
    }

    [HttpPut("{id}/pin")]
    [ProducesResponseType(typeof(SavedViewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SavedViewDto>> SetPinned(int id, PinSavedViewDto dto, CancellationToken ct)
    {
        try
        {
            var view = await savedViewService.SetPinnedAsync(id, UserId, dto.Pinned, ct);
            return view is null ? NotFound() : Ok(view);
        }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct) =>
        await savedViewService.DeleteAsync(id, UserId, ct) ? NoContent() : NotFound();
}
