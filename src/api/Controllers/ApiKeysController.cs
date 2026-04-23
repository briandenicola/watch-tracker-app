using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WatchTracker.Api.DTOs;
using WatchTracker.Api.Services;

namespace WatchTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ApiKeysController(IApiKeyService apiKeyService) : ControllerBase
{
    private int UserId => int.Parse(
        User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    [ProducesResponseType(typeof(List<ApiKeyDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ApiKeyDto>>> GetAll(CancellationToken ct)
    {
        var keys = await apiKeyService.GetAllAsync(UserId, ct);
        return Ok(keys);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiKeyCreatedDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiKeyCreatedDto>> Create(CreateApiKeyDto dto, CancellationToken ct)
    {
        var result = await apiKeyService.CreateAsync(UserId, dto, ct);
        return CreatedAtAction(nameof(GetAll), result);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var deleted = await apiKeyService.DeleteAsync(id, UserId, ct);
        return deleted ? NoContent() : NotFound();
    }
}
