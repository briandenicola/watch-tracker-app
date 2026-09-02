using System.Globalization;

namespace WatchTracker.Api.Services;

/// <summary>
/// Resolves paths inside the uploads directory. Files are stored per owner in
/// <c>uploads/{userId}/</c>, and <see cref="Models.WatchImage.FileName"/> and
/// <see cref="Models.User.ProfileImage"/> hold the <c>{userId}/{name}</c> relative
/// path, which is also what <c>/uploads/{name}</c> URLs are built from. Records
/// written before the per-user layout hold a bare file name that still resolves at
/// the uploads root, so reads fall back there.
/// </summary>
public interface IUploadStorage
{
    /// <summary>Absolute path of the uploads directory.</summary>
    string RootPath { get; }

    /// <summary>Absolute path of a user's own directory, creating it if needed.</summary>
    string EnsureUserDirectory(int userId);

    /// <summary>The value to persist for a file saved in the user's directory.</summary>
    string StoredName(int userId, string fileName);

    /// <summary>
    /// Locates a stored file on disk, falling back to the uploads root for records
    /// that predate the per-user layout. False when nothing is there.
    /// </summary>
    bool TryGetFilePath(string storedName, out string path);
}

public class UploadStorage : IUploadStorage
{
    public UploadStorage(IWebHostEnvironment env)
    {
        RootPath = Path.GetFullPath(Path.Combine(env.ContentRootPath, "uploads"));
    }

    public string RootPath { get; }

    public string EnsureUserDirectory(int userId)
    {
        var directory = Path.Combine(RootPath, userId.ToString(CultureInfo.InvariantCulture));
        Directory.CreateDirectory(directory);
        return directory;
    }

    public string StoredName(int userId, string fileName) =>
        $"{userId.ToString(CultureInfo.InvariantCulture)}/{fileName}";

    public bool TryGetFilePath(string storedName, out string path)
    {
        path = string.Empty;
        if (string.IsNullOrWhiteSpace(storedName)) return false;

        if (TryResolve(storedName, out var candidate) && File.Exists(candidate))
        {
            path = candidate;
            return true;
        }

        // Written before images moved into per-user directories.
        var bareName = Path.GetFileName(Normalize(storedName));
        if (bareName != storedName
            && TryResolve(bareName, out var legacy)
            && File.Exists(legacy))
        {
            path = legacy;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Turns a stored name into an absolute path, refusing anything that resolves
    /// outside the uploads directory.
    /// </summary>
    private bool TryResolve(string storedName, out string path)
    {
        path = string.Empty;
        var relative = Normalize(storedName);
        if (relative.Length == 0 || Path.IsPathRooted(relative)) return false;

        var full = Path.GetFullPath(Path.Combine(RootPath, relative));
        if (!full.StartsWith(RootPath + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            return false;

        path = full;
        return true;
    }

    private static string Normalize(string storedName) =>
        storedName.Replace('\\', '/').Trim('/');
}
