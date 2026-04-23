using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WatchTracker.Api.DTOs;
using WatchTracker.Api.Services;

namespace WatchTracker.Api.Controllers;

[ApiController]
[Route("api/watches/{watchId}/images")]
[Authorize]
public class WatchImagesController(IWatchImageService imageService) : ControllerBase
{
    private int UserId => int.Parse(
        User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost]
    [ProducesResponseType(typeof(List<WatchImageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<WatchImageDto>>> Upload(int watchId, [FromForm] List<IFormFile> files, CancellationToken ct)
    {
        if (files.Count == 0)
            return BadRequest(new { error = "No files provided." });

        try
        {
            var images = await imageService.UploadAsync(watchId, UserId, files, ct);
            return Ok(images);
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    [HttpDelete("{imageId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int watchId, int imageId, CancellationToken ct)
    {
        var result = await imageService.DeleteAsync(imageId, UserId, ct);
        return result ? NoContent() : NotFound();
    }

    [HttpPut("{imageId}/cover")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetCover(int watchId, int imageId, CancellationToken ct)
    {
        var result = await imageService.SetCoverAsync(watchId, imageId, UserId, ct);
        return result ? NoContent() : NotFound();
    }

    [HttpPost("import-url")]
    [ProducesResponseType(typeof(WatchImageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<WatchImageDto>> ImportFromUrl(int watchId, [FromBody] ImportImageUrlDto dto, CancellationToken ct)
    {
        try
        {
            var image = await imageService.ImportFromUrlAsync(watchId, UserId, dto.Url, ct);
            return image is null ? BadRequest(new { error = "Could not download image." }) : Ok(image);
        }
        catch
        {
            return BadRequest(new { error = "Failed to download image from the provided URL." });
        }
    }

    [HttpPost("{imageId}/remove-background")]
    [ProducesResponseType(typeof(WatchImageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(StatusCodes.Status504GatewayTimeout)]
    public async Task<ActionResult<WatchImageDto>> RemoveBackground(int watchId, int imageId, CancellationToken cancellationToken)
    {
        try
        {
            var result = await imageService.RemoveBackgroundAsync(watchId, imageId, UserId, cancellationToken);
            return result is null ? NotFound() : Ok(result);
        }
        catch (FileNotFoundException)
        {
            return Conflict(new { error = "Source image file is missing from disk." });
        }
        catch (TimeoutException)
        {
            return StatusCode(504, new { error = "Background removal timed out. Please try again." });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(503, new { error = ex.Message });
        }
    }
}
