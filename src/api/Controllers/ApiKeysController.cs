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
    public async Task<ActionResult<List<ApiKeyDto>>> GetAll(CancellationToken ct)
    {
        var keys = await apiKeyService.GetAllAsync(UserId, ct);
        return Ok(keys);
    }

    [HttpPost]
    public async Task<ActionResult<ApiKeyCreatedDto>> Create(CreateApiKeyDto dto, CancellationToken ct)
    {
        var result = await apiKeyService.CreateAsync(UserId, dto, ct);
        return CreatedAtAction(nameof(GetAll), result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var deleted = await apiKeyService.DeleteAsync(id, UserId, ct);
        return deleted ? NoContent() : NotFound();
    }
}
