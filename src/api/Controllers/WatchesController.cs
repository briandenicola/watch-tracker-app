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
    public async Task<ActionResult<IEnumerable<WatchDto>>> GetAll()
    {
        var watches = await watchService.GetAllAsync(UserId);
        return Ok(watches);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<WatchDto>> GetById(int id)
    {
        var watch = await watchService.GetByIdAsync(id, UserId);
        return watch is null ? NotFound() : Ok(watch);
    }

    [HttpPost]
    public async Task<ActionResult<WatchDto>> Create(CreateWatchDto dto)
    {
        var watch = await watchService.CreateAsync(dto, UserId);
        return CreatedAtAction(nameof(GetById), new { id = watch.Id }, watch);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<WatchDto>> Update(int id, UpdateWatchDto dto)
    {
        var watch = await watchService.UpdateAsync(id, dto, UserId);
        return watch is null ? NotFound() : Ok(watch);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await watchService.DeleteAsync(id, UserId);
        return result ? NoContent() : NotFound();
    }

    [HttpPost("{id}/analyze")]
    public async Task<ActionResult<object>> Analyze(int id)
    {
        try
        {
            var analysis = await analysisService.AnalyzeAsync(id, UserId);
            return Ok(new { analysis });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
