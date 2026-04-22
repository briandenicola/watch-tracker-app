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
    public async Task<ActionResult<List<WatchImageDto>>> Upload(int watchId, [FromForm] List<IFormFile> files)
    {
        if (files.Count == 0)
            return BadRequest("No files provided.");

        try
        {
            var images = await imageService.UploadAsync(watchId, UserId, files);
            return Ok(images);
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    [HttpDelete("{imageId}")]
    public async Task<IActionResult> Delete(int watchId, int imageId)
    {
        var result = await imageService.DeleteAsync(imageId, UserId);
        return result ? NoContent() : NotFound();
    }

    [HttpPut("{imageId}/cover")]
    public async Task<IActionResult> SetCover(int watchId, int imageId)
    {
        var result = await imageService.SetCoverAsync(watchId, imageId, UserId);
        return result ? NoContent() : NotFound();
    }

    [HttpPost("import-url")]
    public async Task<ActionResult<WatchImageDto>> ImportFromUrl(int watchId, [FromBody] ImportImageUrlDto dto)
    {
        try
        {
            var image = await imageService.ImportFromUrlAsync(watchId, UserId, dto.Url);
            return image is null ? BadRequest("Could not download image.") : Ok(image);
        }
        catch
        {
            return BadRequest("Failed to download image from the provided URL.");
        }
    }

    [HttpPost("{imageId}/remove-background")]
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
