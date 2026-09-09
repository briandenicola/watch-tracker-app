using Microsoft.EntityFrameworkCore;
using WatchTracker.Api.Data;
using WatchTracker.Api.DTOs;

namespace WatchTracker.Api.Services;

public class AdminService(
    AppDbContext context,
    IUploadStorage storage,
    ILogger<AdminService> logger) : IAdminService
{
    public async Task<List<UserDto>> ListUsersAsync(int currentUserId)
    {
        return await context.Users
            .Select(u => new UserDto
            {
                Id = u.Id,
                Username = u.Username,
                Email = u.Email,
                Role = u.Role,
                IsLockedOut = u.LockoutEnd != null && u.LockoutEnd > DateTime.UtcNow,
                FailedLoginAttempts = u.FailedLoginAttempts,
                CreatedAt = u.CreatedAt,
                IsCurrentUser = u.Id == currentUserId
            })
            .ToListAsync();
    }

    public async Task<bool> UnlockUserAsync(int userId)
    {
        var user = await context.Users.FindAsync(userId);
        if (user is null) return false;

        user.FailedLoginAttempts = 0;
        user.LockoutEnd = null;
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ResetPasswordAsync(int userId, string newPassword)
    {
        var user = await context.Users.FindAsync(userId);
        if (user is null) return false;

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.FailedLoginAttempts = 0;
        user.LockoutEnd = null;
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<DeleteUserResult> DeleteUserAsync(int userId, int currentUserId)
    {
        if (userId == currentUserId) return DeleteUserResult.CannotDeleteSelf;

        var user = await context.Users
            .Include(u => u.Watches)
                .ThenInclude(w => w.Images)
            .FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null) return DeleteUserResult.NotFound;

        var storedFiles = user.Watches
            .SelectMany(w => w.Images)
            .Select(i => i.FileName)
            .Append(user.ProfileImage)
            .OfType<string>()
            .Distinct(StringComparer.Ordinal)
            .ToList();

        context.Users.Remove(user);
        await context.SaveChangesAsync();

        foreach (var storedFile in storedFiles)
        {
            if (!storage.TryGetFilePath(storedFile, out var path)) continue;

            try
            {
                File.Delete(path);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                logger.LogError(
                    ex,
                    "Could not delete uploaded file {StoredFile} belonging to deleted user {UserId}.",
                    storedFile,
                    userId);
            }
        }

        return DeleteUserResult.Deleted;
    }
}
