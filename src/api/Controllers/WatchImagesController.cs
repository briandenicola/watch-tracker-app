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
}
