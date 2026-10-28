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
        try
        {
            var result = await apiKeyService.CreateAsync(UserId, dto, ct);
            return CreatedAtAction(nameof(GetAll), result);
        }
        catch (ArgumentException ex)
        {
            return ValidationProblem(ex.Message);
        }
    }

    /// <summary>Changes a key's access (read, read,write or read,agents) without replacing it.</summary>
    [HttpPatch("{id}")]
    [ProducesResponseType(typeof(ApiKeyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiKeyDto>> Update(int id, UpdateApiKeyDto dto, CancellationToken ct)
    {
        try
        {
            var key = await apiKeyService.UpdateScopesAsync(id, UserId, dto.Scopes, ct);
            return key is null ? NotFound() : Ok(key);
        }
        catch (ArgumentException ex)
        {
            return ValidationProblem(ex.Message);
        }
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
