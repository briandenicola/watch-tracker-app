using System.Net;
using Microsoft.EntityFrameworkCore;
using WatchTracker.Api.Data;
using WatchTracker.Api.DTOs;
using WatchTracker.Api.Models;

namespace WatchTracker.Api.Services;

public class WatchImageService(AppDbContext context, IWebHostEnvironment env, IHttpClientFactory httpClientFactory, IBackgroundRemovalService bgRemoval) : IWatchImageService
{
    private const int MaxRemoteImageBytes = 10 * 1024 * 1024;
    private const int MaxRedirects = 3;
    private static readonly HashSet<string> AllowedTypes = ["image/jpeg", "image/png", "image/webp", "image/gif"];

    private static readonly Dictionary<string, string> MimeToExt = new()
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp",
        ["image/gif"] = ".gif",
    };

    public async Task<List<WatchImageDto>> UploadAsync(int watchId, int userId, IEnumerable<IFormFile> files, CancellationToken ct = default)
    {
        var watch = await context.Watches
            .Include(w => w.Images)
            .FirstOrDefaultAsync(w => w.Id == watchId && w.UserId == userId, ct)
            ?? throw new InvalidOperationException("Watch not found.");

        var uploadsDir = Path.Combine(env.ContentRootPath, "uploads");
        Directory.CreateDirectory(uploadsDir);

        var maxSort = watch.Images.Count > 0 ? watch.Images.Max(i => i.SortOrder) : -1;
        var result = new List<WatchImageDto>();

        foreach (var file in files)
        {
            if (!AllowedTypes.Contains(file.ContentType))
                continue;

            // Verify actual file content matches claimed content type via magic bytes
            using var peekStream = file.OpenReadStream();
            var header = new byte[12];
            var bytesRead = await peekStream.ReadAsync(header.AsMemory(0, 12));
            peekStream.Position = 0;

            var detectedType = DetectImageType(header);
            if (detectedType is null || !AllowedTypes.Contains(detectedType))
                continue;

            var ext = MimeToExt.GetValueOrDefault(detectedType, Path.GetExtension(file.FileName));
            var fileName = $"{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(uploadsDir, fileName);

            await using var stream = new FileStream(filePath, FileMode.Create);
            await peekStream.CopyToAsync(stream);

            var image = new WatchImage
            {
                WatchId = watchId,
                FileName = fileName,
                ContentType = detectedType,
                SortOrder = ++maxSort
            };

            context.WatchImages.Add(image);
            await context.SaveChangesAsync(ct);

            result.Add(new WatchImageDto { Id = image.Id, Url = $"/uploads/{fileName}" });
        }

        return result;
    }

    public async Task<WatchImageDto?> ImportFromUrlAsync(int watchId, int userId, string imageUrl, CancellationToken ct = default)
    {
        var watch = await context.Watches
            .Include(w => w.Images)
            .FirstOrDefaultAsync(w => w.Id == watchId && w.UserId == userId, ct);

        if (watch is null) return null;

        if (!TryParseWebUrl(imageUrl, out var uri))
            throw new InvalidOperationException("Image URL must use HTTP or HTTPS.");

        var httpClient = httpClientFactory.CreateClient("RemoteImage");
        byte[] bytes;
        using (var response = await DownloadAsync(httpClient, uri, ct))
        {
            bytes = await ReadCappedAsync(response, ct);
        }

        // The remote server and URL are untrusted; only the bytes decide the type.
        var contentType = DetectImageType(bytes);

        if (contentType is null || !AllowedTypes.Contains(contentType))
            return null;

        var ext = MimeToExt.GetValueOrDefault(contentType, ".jpg");
        var fileName = $"{Guid.NewGuid()}{ext}";
        var uploadsDir = Path.Combine(env.ContentRootPath, "uploads");
        Directory.CreateDirectory(uploadsDir);
        var filePath = Path.Combine(uploadsDir, fileName);

        await File.WriteAllBytesAsync(filePath, bytes, ct);
        try
        {
            var maxSort = watch.Images.Count > 0 ? watch.Images.Max(i => i.SortOrder) : -1;
            var image = new WatchImage
            {
                WatchId = watchId,
                FileName = fileName,
                ContentType = contentType,
                SortOrder = ++maxSort,
            };

            context.WatchImages.Add(image);
            await context.SaveChangesAsync(ct);

            return new WatchImageDto { Id = image.Id, Url = $"/uploads/{fileName}" };
        }
        catch
        {
            File.Delete(filePath);
            throw;
        }
    }

    private static async Task<HttpResponseMessage> DownloadAsync(
        HttpClient httpClient,
        Uri initialUri,
        CancellationToken ct)
    {
        var uri = initialUri;
        for (var hop = 0; hop <= MaxRedirects; hop++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.TryAddWithoutValidation("Accept", "image/*");
            var response = await httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                ct);

            if (IsRedirect(response.StatusCode))
            {
                var location = response.Headers.Location;
                response.Dispose();
                if (location is null || hop == MaxRedirects)
                    throw new HttpRequestException("The image URL redirected too many times.");

                var next = location.IsAbsoluteUri ? location : new Uri(uri, location);
                if (!TryParseWebUrl(next.ToString(), out uri))
                    throw new HttpRequestException("The image redirected to an unsupported URL.");
                continue;
            }

            response.EnsureSuccessStatusCode();
            if (response.Content.Headers.ContentLength > MaxRemoteImageBytes)
            {
                response.Dispose();
                throw new HttpRequestException("The remote image is too large.");
            }

            return response;
        }

        throw new HttpRequestException("The image URL redirected too many times.");
    }

    private static async Task<byte[]> ReadCappedAsync(
        HttpResponseMessage response,
        CancellationToken ct)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var output = new MemoryStream();
        var buffer = new byte[81920];
        while (true)
        {
            var read = await stream.ReadAsync(buffer, ct);
            if (read == 0) break;
            if (output.Length + read > MaxRemoteImageBytes)
                throw new HttpRequestException("The remote image is too large.");
            await output.WriteAsync(buffer.AsMemory(0, read), ct);
        }

        return output.ToArray();
    }

    private static bool IsRedirect(HttpStatusCode status) =>
        status is HttpStatusCode.Moved or HttpStatusCode.Found or HttpStatusCode.SeeOther
            or HttpStatusCode.TemporaryRedirect or HttpStatusCode.PermanentRedirect;

    private static bool TryParseWebUrl(string value, out Uri uri)
    {
        uri = null!;
        if (!Uri.TryCreate(value, UriKind.Absolute, out var parsed)) return false;
        if (parsed.Scheme is not ("http" or "https")) return false;
        uri = parsed;
        return true;
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

    public async Task<bool> DeleteAsync(int imageId, int userId, CancellationToken ct = default)
    {
        var image = await context.WatchImages
            .Include(i => i.Watch)
            .FirstOrDefaultAsync(i => i.Id == imageId && i.Watch.UserId == userId, ct);

        if (image is null) return false;

        var filePath = Path.Combine(env.ContentRootPath, "uploads", image.FileName);
        if (File.Exists(filePath))
            File.Delete(filePath);

        context.WatchImages.Remove(image);
        await context.SaveChangesAsync(ct);

        return true;
    }

    public async Task<bool> SetCoverAsync(int watchId, int imageId, int userId, CancellationToken ct = default)
    {
        var watch = await context.Watches
            .Include(w => w.Images)
            .FirstOrDefaultAsync(w => w.Id == watchId && w.UserId == userId, ct);

        if (watch is null) return false;

        var target = watch.Images.FirstOrDefault(i => i.Id == imageId);
        if (target is null) return false;

        target.SortOrder = -1;
        foreach (var img in watch.Images.Where(i => i.Id != imageId).OrderBy(i => i.SortOrder))
        {
            img.SortOrder++;
        }
        target.SortOrder = 0;

        await context.SaveChangesAsync(ct);
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
