using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using WatchTracker.Api.Data;
using WatchTracker.Api.DTOs;
using WatchTracker.Api.Models;

namespace WatchTracker.Api.Services;

public class ApiKeyService(AppDbContext context) : IApiKeyService
{
    private const string KeyPrefix = "wt_";

    public async Task<ApiKeyCreatedDto> CreateAsync(int userId, CreateApiKeyDto dto, CancellationToken ct = default)
    {
        var rawKey = GenerateKey();
        var hash = HashKey(rawKey);

        var apiKey = new ApiKey
        {
            UserId = userId,
            Name = dto.Name,
            KeyHash = hash,
            CreatedAt = DateTime.UtcNow
        };

        context.ApiKeys.Add(apiKey);
        await context.SaveChangesAsync(ct);

        return new ApiKeyCreatedDto
        {
            Id = apiKey.Id,
            Name = apiKey.Name,
            Key = rawKey,
            CreatedAt = apiKey.CreatedAt
        };
    }

    public async Task<List<ApiKeyDto>> GetAllAsync(int userId, CancellationToken ct = default)
    {
        return await context.ApiKeys
            .Where(k => k.UserId == userId)
            .OrderByDescending(k => k.CreatedAt)
            .Select(k => new ApiKeyDto
            {
                Id = k.Id,
                Name = k.Name,
                CreatedAt = k.CreatedAt,
                LastUsedAt = k.LastUsedAt
            })
            .ToListAsync(ct);
    }

    public async Task<bool> DeleteAsync(int id, int userId, CancellationToken ct = default)
    {
        var key = await context.ApiKeys
            .FirstOrDefaultAsync(k => k.Id == id && k.UserId == userId, ct);
        if (key is null) return false;

        context.ApiKeys.Remove(key);
        await context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<User?> ValidateAsync(string rawKey, CancellationToken ct = default)
    {
        var hash = HashKey(rawKey);
        var apiKey = await context.ApiKeys
            .Include(k => k.User)
            .FirstOrDefaultAsync(k => k.KeyHash == hash, ct);

        if (apiKey is null) return null;

        apiKey.LastUsedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(ct);

        return apiKey.User;
    }

    private static string GenerateKey()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return KeyPrefix + Convert.ToBase64String(bytes)
            .Replace("+", "")
            .Replace("/", "")
            .Replace("=", "");
    }

    private static string HashKey(string rawKey)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawKey));
        return Convert.ToHexStringLower(bytes);
    }
}
