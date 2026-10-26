using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using WatchTracker.Api.Authentication;

namespace WatchTracker.Api.Tests;

// API keys only read over the REST API. Before this guard a key shown as
// "Read only" in Settings could add, edit and delete watches.
public class ApiKeyReadOnlyMiddlewareTests
{
    private static async Task<(int Status, bool Passed)> Run(string method, string path, string? authenticationType)
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
                [new Claim(ClaimTypes.NameIdentifier, "1"), new Claim(ApiKeyScopes.ClaimType, "read,agents")],
                authenticationType));
        }
        await middleware.InvokeAsync(context);
        return (context.Response.StatusCode, passed);
    }

    [Theory]
    [InlineData("GET", "/api/watches")]
    [InlineData("HEAD", "/api/watches")]
    [InlineData("OPTIONS", "/api/watches")]
    [InlineData("GET", "/api/data/export")]
    public async Task ApiKeyReadsPass(string method, string path)
    {
        var (_, passed) = await Run(method, path, ApiKeyAuthenticationHandler.SchemeName);
        Assert.True(passed);
    }

    [Theory]
    [InlineData("POST", "/api/watches")]
    [InlineData("PUT", "/api/watches/1")]
    [InlineData("PATCH", "/api/watches/1")]
    [InlineData("DELETE", "/api/watches/1")]
    [InlineData("POST", "/api/apikeys")]
    [InlineData("POST", "/api/data/import")]
    public async Task ApiKeyChangesAreRefused(string method, string path)
    {
        var (status, passed) = await Run(method, path, ApiKeyAuthenticationHandler.SchemeName);
        Assert.False(passed);
        Assert.Equal(StatusCodes.Status403Forbidden, status);
    }

    [Fact]
    public async Task McpIsLeftToItsOwnScopeChecks()
    {
        var (_, passed) = await Run("POST", "/api/mcp", ApiKeyAuthenticationHandler.SchemeName);
        Assert.True(passed);
    }

    [Theory]
    [InlineData("POST", "/api/watches")]
    [InlineData("DELETE", "/api/watches/1")]
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
}
