using Microsoft.EntityFrameworkCore;
using WatchTracker.Api.Data;

namespace WatchTracker.Api.Services;

/// <summary>
/// Moves uploads written before the per-user layout out of the shared uploads root
/// and into <c>uploads/{userId}/</c>, rewriting the stored names to match. Runs on
/// startup and is a no-op once every record carries a per-user path.
/// </summary>
public class UploadLayoutMigrator(
    AppDbContext context,
    IUploadStorage storage,
    ILogger<UploadLayoutMigrator> logger)
{
    public async Task MigrateAsync(CancellationToken ct = default)
    {
        if (!Directory.Exists(storage.RootPath)) return;

        var moved = 0;

        var images = await context.WatchImages
            .Include(i => i.Watch)
            .Where(i => !i.FileName.Contains("/"))
            .ToListAsync(ct);

        foreach (var image in images)
        {
            if (TryMove(image.FileName, image.Watch.UserId, out var storedName))
            {
                image.FileName = storedName;
                moved++;
            }
        }

        var users = await context.Users
            .Where(u => u.ProfileImage != null && !u.ProfileImage.Contains("/"))
            .ToListAsync(ct);

        foreach (var user in users)
        {
            if (TryMove(user.ProfileImage!, user.Id, out var storedName))
            {
                user.ProfileImage = storedName;
                moved++;
            }
        }

        if (moved == 0) return;

        await context.SaveChangesAsync(ct);
        logger.LogInformation("Moved {Count} upload(s) into per-user directories.", moved);
    }

    private bool TryMove(string fileName, int userId, out string storedName)
    {
        storedName = storage.StoredName(userId, fileName);

        var source = Path.Combine(storage.RootPath, fileName);
        var destination = Path.Combine(
            storage.RootPath,
            userId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            fileName);

        if (File.Exists(destination))
        {
            // A previous run moved the file but did not get to update the record.
            if (File.Exists(source)) File.Delete(source);
            return true;
        }

        if (!File.Exists(source))
        {
            logger.LogWarning(
                "Upload {FileName} for user {UserId} is missing from disk; leaving the record as-is.",
                fileName,
                userId);
            return false;
        }

        try
        {
            storage.EnsureUserDirectory(userId);
            File.Move(source, destination);
            return true;
        }
        catch (IOException ex)
        {
            logger.LogError(
                ex,
                "Could not move upload {FileName} into the directory for user {UserId}.",
                fileName,
                userId);
            return false;
        }
    }
}
