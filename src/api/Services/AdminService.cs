using Microsoft.EntityFrameworkCore;
using WatchTracker.Api.Data;
using WatchTracker.Api.DTOs;

namespace WatchTracker.Api.Services;

public class AdminService(AppDbContext context) : IAdminService
{
    public async Task<List<UserDto>> ListUsersAsync()
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
                CreatedAt = u.CreatedAt
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
}
