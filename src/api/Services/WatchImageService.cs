using Microsoft.EntityFrameworkCore;
using WatchTracker.Api.Data;
using WatchTracker.Api.DTOs;
using WatchTracker.Api.Models;

namespace WatchTracker.Api.Services;

public class WatchImageService(AppDbContext context, IWebHostEnvironment env, IHttpClientFactory httpClientFactory, IBackgroundRemovalService bgRemoval) : IWatchImageService
{
    private static readonly HashSet<string> AllowedTypes = ["image/jpeg", "image/png", "image/webp", "image/gif"];

    private static readonly Dictionary<string, string> MimeToExt = new()
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp",
        ["image/gif"] = ".gif",
    };

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

    public async Task<WatchImageDto?> ImportFromUrlAsync(int watchId, int userId, string imageUrl)
    {
        var watch = await context.Watches
            .Include(w => w.Images)
            .FirstOrDefaultAsync(w => w.Id == watchId && w.UserId == userId);

        if (watch is null) return null;

        var httpClient = httpClientFactory.CreateClient();
        using var response = await httpClient.GetAsync(imageUrl);
        response.EnsureSuccessStatusCode();

        var bytes = await response.Content.ReadAsByteArrayAsync();

        // Detect type from magic bytes first, then Content-Type header, then URL extension
        var contentType = DetectImageType(bytes)
            ?? response.Content.Headers.ContentType?.MediaType
            ?? InferFromUrl(imageUrl);

        if (contentType is null || !AllowedTypes.Contains(contentType))
            return null;

        var ext = MimeToExt.GetValueOrDefault(contentType, ".jpg");
        var fileName = $"{Guid.NewGuid()}{ext}";
        var uploadsDir = Path.Combine(env.ContentRootPath, "uploads");
        Directory.CreateDirectory(uploadsDir);
        var filePath = Path.Combine(uploadsDir, fileName);

        await File.WriteAllBytesAsync(filePath, bytes);

        var maxSort = watch.Images.Count > 0 ? watch.Images.Max(i => i.SortOrder) : -1;
        var image = new WatchImage
        {
            WatchId = watchId,
            FileName = fileName,
            ContentType = contentType,
            SortOrder = ++maxSort,
        };

        context.WatchImages.Add(image);
        await context.SaveChangesAsync();

        return new WatchImageDto { Id = image.Id, Url = $"/uploads/{fileName}" };
    }

    private static string? DetectImageType(byte[] data)
    {
        if (data.Length < 12) return null;

        // JPEG: FF D8 FF
        if (data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF)
            return "image/jpeg";

        // PNG: 89 50 4E 47 0D 0A 1A 0A
        if (data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47)
            return "image/png";

        // GIF: 47 49 46 38
        if (data[0] == 0x47 && data[1] == 0x49 && data[2] == 0x46 && data[3] == 0x38)
            return "image/gif";

        // WebP: RIFF....WEBP
        if (data[0] == 0x52 && data[1] == 0x49 && data[2] == 0x46 && data[3] == 0x46
            && data[8] == 0x57 && data[9] == 0x45 && data[10] == 0x42 && data[11] == 0x50)
            return "image/webp";

        return null;
    }

    private static string? InferFromUrl(string imageUrl)
    {
        try
        {
            var ext = Path.GetExtension(new Uri(imageUrl).AbsolutePath).ToLowerInvariant();
            return ext switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".webp" => "image/webp",
                ".gif" => "image/gif",
                _ => null,
            };
        }
        catch { return null; }
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

    public async Task<bool> SetCoverAsync(int watchId, int imageId, int userId)
    {
        var watch = await context.Watches
            .Include(w => w.Images)
            .FirstOrDefaultAsync(w => w.Id == watchId && w.UserId == userId);

        if (watch is null) return false;

        var target = watch.Images.FirstOrDefault(i => i.Id == imageId);
        if (target is null) return false;

        target.SortOrder = -1;
        foreach (var img in watch.Images.Where(i => i.Id != imageId).OrderBy(i => i.SortOrder))
        {
            img.SortOrder++;
        }
        target.SortOrder = 0;

        await context.SaveChangesAsync();
        return true;
    }

    public async Task<WatchImageDto?> RemoveBackgroundAsync(int watchId, int imageId, int userId, CancellationToken cancellationToken = default)
    {
        var image = await context.WatchImages
            .Include(i => i.Watch)
            .FirstOrDefaultAsync(i => i.Id == imageId && i.WatchId == watchId && i.Watch.UserId == userId, cancellationToken);

        if (image is null) return null;

        var uploadsDir = Path.Combine(env.ContentRootPath, "uploads");
        var inputPath = Path.Combine(uploadsDir, image.FileName);

        if (!File.Exists(inputPath))
            throw new FileNotFoundException("Source image file not found on disk.");

        var newFileName = await bgRemoval.RemoveBackgroundAsync(inputPath, cancellationToken);
        var newFilePath = Path.Combine(uploadsDir, newFileName);

        try
        {
            var oldFileName = image.FileName;
            image.FileName = newFileName;
            image.ContentType = "image/png";
            await context.SaveChangesAsync(cancellationToken);

            // Only delete old file after DB update succeeds
            var oldFilePath = Path.Combine(uploadsDir, oldFileName);
            if (File.Exists(oldFilePath))
                File.Delete(oldFilePath);
        }
        catch
        {
            // DB update failed — clean up the new file
            if (File.Exists(newFilePath))
                File.Delete(newFilePath);
            throw;
        }

        return new WatchImageDto { Id = image.Id, Url = $"/uploads/{newFileName}" };
    }
}
