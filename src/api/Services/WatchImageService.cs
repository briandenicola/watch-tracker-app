using Microsoft.EntityFrameworkCore;
using WatchTracker.Api.Data;
using WatchTracker.Api.DTOs;
using WatchTracker.Api.Models;

namespace WatchTracker.Api.Services;

public class WatchImageService(AppDbContext context, IWebHostEnvironment env) : IWatchImageService
{
    private static readonly HashSet<string> AllowedTypes = ["image/jpeg", "image/png", "image/webp", "image/gif"];

    public async Task<List<WatchImageDto>> UploadAsync(int watchId, int userId, IEnumerable<IFormFile> files)
    {
        var watch = await context.Watches
            .Include(w => w.Images)
            .FirstOrDefaultAsync(w => w.Id == watchId && w.UserId == userId)
            ?? throw new InvalidOperationException("Watch not found.");

        var uploadsDir = Path.Combine(env.ContentRootPath, "uploads");
        Directory.CreateDirectory(uploadsDir);

        var maxSort = watch.Images.Count > 0 ? watch.Images.Max(i => i.SortOrder) : -1;
        var result = new List<WatchImageDto>();

        foreach (var file in files)
        {
            if (!AllowedTypes.Contains(file.ContentType))
                continue;

            var ext = Path.GetExtension(file.FileName);
            var fileName = $"{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(uploadsDir, fileName);

            await using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);

            var image = new WatchImage
            {
                WatchId = watchId,
                FileName = fileName,
                ContentType = file.ContentType,
                SortOrder = ++maxSort
            };

            context.WatchImages.Add(image);
            await context.SaveChangesAsync();

            result.Add(new WatchImageDto { Id = image.Id, Url = $"/uploads/{fileName}" });
        }

        return result;
    }

    public async Task<bool> DeleteAsync(int imageId, int userId)
    {
        var image = await context.WatchImages
            .Include(i => i.Watch)
            .FirstOrDefaultAsync(i => i.Id == imageId && i.Watch.UserId == userId);

        if (image is null) return false;

        var filePath = Path.Combine(env.ContentRootPath, "uploads", image.FileName);
        if (File.Exists(filePath))
            File.Delete(filePath);

        context.WatchImages.Remove(image);
        await context.SaveChangesAsync();

        return true;
    }
}
