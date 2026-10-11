using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using WatchTracker.Api.Data;
using WatchTracker.Api.DTOs;
using WatchTracker.Api.Models;

namespace WatchTracker.Api.Services;

public class AuthService(AppDbContext context, IConfiguration configuration, IAppSettingsService appSettings) : IAuthService
{
    public async Task<AuthResponseDto?> RegisterAsync(RegisterDto dto)
    {
        if (await context.Users.AnyAsync(u => u.Email == dto.Email))
            return null;

        var user = new User
        {
            Username = dto.Username,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = UserRole.Standard,
            CreatedAt = DateTime.UtcNow
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var response = BuildResponse(user);
        response.RefreshToken = await CreateRefreshTokenAsync(user.Id);
        return response;
    }

    public async Task<AuthResponseDto?> LoginAsync(LoginDto dto)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (user is null)
            return null;

        // Check lockout
        if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTime.UtcNow)
            return null;

        // Auto-unlock if lockout expired
        if (user.LockoutEnd.HasValue && user.LockoutEnd <= DateTime.UtcNow)
        {
            user.LockoutEnd = null;
            user.FailedLoginAttempts = 0;
        }

        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        {
            user.FailedLoginAttempts++;

            var maxAttempts = await appSettings.GetIntAsync(
                AppSettingsService.Keys.MaxFailedAttempts, 5);
            if (user.FailedLoginAttempts >= maxAttempts)
            {
                var lockoutMinutes = await appSettings.GetIntAsync(
                    AppSettingsService.Keys.LockoutDurationMinutes, 15);
                user.LockoutEnd = DateTime.UtcNow.AddMinutes(lockoutMinutes);
            }

            await context.SaveChangesAsync();
            return null;
        }

        // Successful login — clear failed attempts
        user.FailedLoginAttempts = 0;
        user.LockoutEnd = null;
        await context.SaveChangesAsync();

        var response = BuildResponse(user);
        response.RefreshToken = await CreateRefreshTokenAsync(user.Id);
        return response;
    }

    public async Task<AuthResponseDto?> RefreshAsync(string refreshToken)
    {
        var hash = HashToken(refreshToken);
        var stored = await context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.TokenHash == hash);

        if (stored is null || !stored.IsActive)
            return null;

        // Revoke old token (rotation)
        stored.RevokedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        // Issue new token pair
        var response = BuildResponse(stored.User);
        response.RefreshToken = await CreateRefreshTokenAsync(stored.UserId);
        return response;
    }

    public async Task RevokeRefreshTokenAsync(string refreshToken)
    {
        var hash = HashToken(refreshToken);
        var stored = await context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.TokenHash == hash);

        if (stored is not null)
        {
            stored.RevokedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }
    }

    public async Task<AuthResponseDto?> IssueTokensForUserAsync(int userId)
    {
        var user = await context.Users.FindAsync(userId);
        if (user is null) return null;

        var response = BuildResponse(user);
        response.RefreshToken = await CreateRefreshTokenAsync(user.Id);
        return response;
    }

    private AuthResponseDto BuildResponse(User user) => new()
    {
        Token = GenerateAccessToken(user),
        Username = user.Username,
        Email = user.Email,
        Role = user.Role.ToString(),
        ProfileImage = user.ProfileImage is not null ? $"/uploads/{user.ProfileImage}" : null,
        StorageLocations = ParseStorageLocations(user.StorageLocationsJson),
        StorageLayouts = StorageLayouts.Parse(user.StorageLayoutsJson)
            .ToDictionary(kv => kv.Key, kv => kv.Value)
    };

    private string GenerateAccessToken(User user)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task<string> CreateRefreshTokenAsync(int userId)
    {
        var tokenBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(tokenBytes);
        var token = Convert.ToBase64String(tokenBytes);

        context.RefreshTokens.Add(new RefreshToken
        {
            TokenHash = HashToken(token),
            UserId = userId,
            ExpiresAt = DateTime.UtcNow.AddDays(90),
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();
        return token;
    }

    private static string HashToken(string token) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    public async Task<bool> ChangePasswordAsync(int userId, ChangePasswordDto dto)
    {
        var user = await context.Users.FindAsync(userId);
        if (user is null) return false;

        if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
            return false;

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<AuthResponseDto?> GetProfileAsync(int userId)
    {
        var user = await context.Users.FindAsync(userId);
        return user is null ? null : BuildResponse(user);
    }

    public async Task SetProfileImageAsync(int userId, string fileName)
    {
        var user = await context.Users.FindAsync(userId);
        if (user is null) return;
        user.ProfileImage = fileName;
        await context.SaveChangesAsync();
    }

    public async Task<string?> DeleteProfileImageAsync(int userId)
    {
        var user = await context.Users.FindAsync(userId);
        if (user?.ProfileImage is null) return null;
        var old = user.ProfileImage;
        user.ProfileImage = null;
        await context.SaveChangesAsync();
        return old;
    }

    public async Task<bool> UpdateUsernameAsync(int userId, string username)
    {
        var user = await context.Users.FindAsync(userId);
        if (user is null) return false;
        user.Username = username;
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<StorageSettingsDto?> UpdateStorageLocationsAsync(
        int userId,
        IEnumerable<string> storageLocations,
        Dictionary<string, StorageLayoutDto>? storageLayouts)
    {
        var user = await context.Users.FindAsync(userId);
        if (user is null) return null;

        var normalized = NormalizeStorageLocations(storageLocations);
        var requested = storageLayouts is null
            ? StorageLayouts.Parse(user.StorageLayoutsJson)
            : new Dictionary<string, StorageLayoutDto>(storageLayouts, StringComparer.OrdinalIgnoreCase);

        var layouts = new Dictionary<string, StorageLayoutDto>();
        foreach (var location in normalized)
        {
            if (requested.TryGetValue(location, out var layout) && StorageLayouts.IsValid(layout))
                layouts[location] = layout;
        }

        await EnsureWatchesFitAsync(userId, layouts);

        user.StorageLocationsJson = JsonSerializer.Serialize(normalized);
        user.StorageLayoutsJson = JsonSerializer.Serialize(layouts);
        await context.SaveChangesAsync();
        return new StorageSettingsDto { StorageLocations = normalized, StorageLayouts = layouts };
    }

    private async Task EnsureWatchesFitAsync(int userId, Dictionary<string, StorageLayoutDto> layouts)
    {
        foreach (var (location, layout) in layouts)
        {
            var capacity = StorageLayouts.Capacity(layout);
            var slots = await context.Watches
                .Where(w => w.UserId == userId && !w.IsWishList && w.Disposition == null && w.StorageLocation == location)
                .Select(w => w.StorageSlot)
                .ToListAsync();

            if (slots.Count > capacity || slots.Any(s => s >= capacity))
                throw new InvalidOperationException(
                    $"'{location}' holds {slots.Count} watches and cannot be resized to {layout.Rows}x{layout.Columns}. Move watches out or pick a larger grid.");
        }
    }

    private static List<string> NormalizeStorageLocations(IEnumerable<string> storageLocations)
    {
        var normalized = new List<string>();
        foreach (var location in storageLocations.Select(l => l.Trim()).Where(l => !string.IsNullOrWhiteSpace(l)))
        {
            if (normalized.Any(l => string.Equals(l, location, StringComparison.OrdinalIgnoreCase)))
                continue;

            normalized.Add(location);
            if (normalized.Count == 50)
                break;
        }

        return normalized;
    }

    private static List<string> ParseStorageLocations(string? storageLocationsJson)
    {
        if (string.IsNullOrWhiteSpace(storageLocationsJson))
            return [];

        var locations = JsonSerializer.Deserialize<List<string>>(storageLocationsJson) ?? [];
        return NormalizeStorageLocations(locations);
    }
}
