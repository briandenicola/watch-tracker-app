using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
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

    private AuthResponseDto BuildResponse(User user) => new()
    {
        Token = GenerateAccessToken(user),
        Username = user.Username,
        Email = user.Email,
        Role = user.Role.ToString(),
        ProfileImage = user.ProfileImage is not null ? $"/uploads/{user.ProfileImage}" : null
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
}
