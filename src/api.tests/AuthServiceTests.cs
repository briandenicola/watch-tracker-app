using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using WatchTracker.Api.DTOs;
using WatchTracker.Api.Models;
using WatchTracker.Api.Services;

namespace WatchTracker.Api.Tests;

public class AuthServiceTests
{
    [Fact]
    public async Task Refresh_rotates_token_and_preserves_access_token_claims()
    {
        await using var database = await TestDatabase.CreateAsync();
        var user = TestDatabase.User("collector");
        user.Role = UserRole.Admin;
        database.Context.Users.Add(user);
        await database.Context.SaveChangesAsync();
        var service = CreateService(database);

        var issued = await service.IssueTokensForUserAsync(user.Id);
        var initialRefreshToken = issued!.RefreshToken!;
        var refreshed = await service.RefreshAsync(initialRefreshToken);

        Assert.NotNull(refreshed);
        Assert.NotEqual(initialRefreshToken, refreshed.RefreshToken);
        Assert.Null(await service.RefreshAsync(initialRefreshToken));

        var stored = await database.Context.RefreshTokens.OrderBy(t => t.Id).ToListAsync();
        Assert.Equal(2, stored.Count);
        Assert.NotNull(stored.Single(t => t.RevokedAt is not null).RevokedAt);
        Assert.Single(stored, token => token.IsActive);

        var claims = new JwtSecurityTokenHandler().ReadJwtToken(refreshed.Token).Claims;
        Assert.Equal(user.Id.ToString(), claims.Single(c => c.Type == ClaimTypes.NameIdentifier).Value);
        Assert.Equal(user.Username, claims.Single(c => c.Type == ClaimTypes.Name).Value);
        Assert.Equal(user.Email, claims.Single(c => c.Type == ClaimTypes.Email).Value);
        Assert.Equal(UserRole.Admin.ToString(), claims.Single(c => c.Type == ClaimTypes.Role).Value);
    }

    private static AuthService CreateService(TestDatabase database)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "a-very-long-test-key-that-is-at-least-thirty-two-characters",
            ["Jwt:Issuer"] = "watch-tracker-tests",
            ["Jwt:Audience"] = "watch-tracker-tests"
        }).Build();
        return new AuthService(database.Context, configuration, new AppSettingsService(database.Context));
    }
}
