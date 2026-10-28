using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using WatchTracker.Api.Authentication;
using WatchTracker.Api.Services;

namespace WatchTracker.Api.Tests;

// Over the REST API any key reads; only a key with the write scope (Read +
// write in Settings) changes data, and no key manages API keys.
public class ApiKeyReadOnlyMiddlewareTests
{
    private static async Task<(int Status, bool Passed)> Run(string method, string path, string? authenticationType, string scopes = "read,agents")
    {
        var passed = false;
        var middleware = new ApiKeyReadOnlyMiddleware(_ =>
        {
            passed = true;
            return Task.CompletedTask;
        });
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();
        if (authenticationType is not null)
        {
            context.User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, "1"), new Claim(ApiKeyScopes.ClaimType, scopes)],
                authenticationType));
        }
        await middleware.InvokeAsync(context);
        return (context.Response.StatusCode, passed);
    }

    [Theory]
    [InlineData("GET", "/api/watches", "read")]
    [InlineData("HEAD", "/api/watches", "read")]
    [InlineData("OPTIONS", "/api/watches", "read,agents")]
    [InlineData("GET", "/api/data/export", "read,write")]
    public async Task EveryKeyReads(string method, string path, string scopes)
    {
        var (_, passed) = await Run(method, path, ApiKeyAuthenticationHandler.SchemeName, scopes);
        Assert.True(passed);
    }

    [Theory]
    [InlineData("POST", "/api/watches", "read")]
    [InlineData("PUT", "/api/watches/1", "read")]
    [InlineData("PATCH", "/api/watches/1", "read,agents")]
    [InlineData("DELETE", "/api/watches/1", "read,agents")]
    [InlineData("POST", "/api/data/import", "read")]
    public async Task KeysWithoutWriteCannotChangeData(string method, string path, string scopes)
    {
        var (status, passed) = await Run(method, path, ApiKeyAuthenticationHandler.SchemeName, scopes);
        Assert.False(passed);
        Assert.Equal(StatusCodes.Status403Forbidden, status);
    }

    [Theory]
    [InlineData("POST", "/api/watches", "read,write")]
    [InlineData("PUT", "/api/watches/1", "read,write")]
    [InlineData("PATCH", "/api/watches/1", "read,write,agents")]
    [InlineData("DELETE", "/api/watches/1", "read,write")]
    [InlineData("POST", "/api/data/import", "read,write")]
    public async Task WriteKeysChangeData(string method, string path, string scopes)
    {
        var (_, passed) = await Run(method, path, ApiKeyAuthenticationHandler.SchemeName, scopes);
        Assert.True(passed);
    }

    [Theory]
    [InlineData("POST", "/api/apikeys")]
    [InlineData("PATCH", "/api/apikeys/1")]
    [InlineData("DELETE", "/api/apikeys/1")]
    public async Task NoKeyManagesKeys(string method, string path)
    {
        var (status, passed) = await Run(method, path, ApiKeyAuthenticationHandler.SchemeName, "read,write,agents");
        Assert.False(passed);
        Assert.Equal(StatusCodes.Status403Forbidden, status);
    }

    [Fact]
    public async Task McpIsLeftToItsOwnScopeChecks()
    {
        var (_, passed) = await Run("POST", "/api/mcp", ApiKeyAuthenticationHandler.SchemeName, "read");
        Assert.True(passed);
    }

    [Theory]
    [InlineData("POST", "/api/watches")]
    [InlineData("DELETE", "/api/watches/1")]
    [InlineData("PATCH", "/api/apikeys/1")]
    public async Task SignedInUsersAreUnchanged(string method, string path)
    {
        var (_, passed) = await Run(method, path, "AuthenticationTypes.Federation");
        Assert.True(passed);
    }

    [Fact]
    public async Task AnonymousRequestsAreLeftToAuthorization()
    {
        var (_, passed) = await Run("POST", "/api/auth/login", null);
        Assert.True(passed);
    }

    // A key an app already uses can be given write access in place: the key
    // stays the same, only what it may do changes.
    [Fact]
    public async Task AnExistingKeyCanBeGivenWriteAccess()
    {
        await using var db = await TestDatabase.CreateAsync();
        var owner = new WatchTracker.Api.Models.User { Username = "brian", Email = "b@example.com", PasswordHash = "x" };
        var other = new WatchTracker.Api.Models.User { Username = "nelie", Email = "n@example.com", PasswordHash = "x" };
        db.Context.Users.AddRange(owner, other);
        await db.Context.SaveChangesAsync();
        var service = new ApiKeyService(db.Context);
        var created = await service.CreateAsync(owner.Id, new() { Name = "sync app", Scopes = "read" });

        var updated = await service.UpdateScopesAsync(created.Id, owner.Id, "read,write");
        Assert.Equal("read,write", updated?.Scopes);
        var validated = await service.ValidateWithScopesAsync(created.Key);
        Assert.Equal("read,write", validated?.Scopes);

        Assert.Null(await service.UpdateScopesAsync(created.Id, other.Id, "read,write"));
        await Assert.ThrowsAsync<ArgumentException>(() => service.UpdateScopesAsync(created.Id, owner.Id, "admin"));
    }
}
