using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using ModelContextProtocol;
using WatchTracker.Api.Authentication;
using WatchTracker.Api.Mcp;

namespace WatchTracker.Api.Tests;

public class McpScopeTests
{
    [Theory]
    [InlineData(null, "read")]
    [InlineData("", "read")]
    [InlineData("read", "read")]
    [InlineData("agents", "read,agents")]
    [InlineData("Read, Agents", "read,agents")]
    [InlineData("agents,read,agents", "read,agents")]
    [InlineData("admin", null)]
    [InlineData("read,write", null)]
    public void Normalize_returns_canonical_scopes_or_null(string? input, string? expected) =>
        Assert.Equal(expected, ApiKeyScopes.Normalize(input));

    [Fact]
    public void Has_checks_each_scope() 
    {
        Assert.True(ApiKeyScopes.Has("read,agents", ApiKeyScopes.Agents));
        Assert.False(ApiKeyScopes.Has("read", ApiKeyScopes.Agents));
        Assert.False(ApiKeyScopes.Has("", ApiKeyScopes.Read));
        Assert.False(ApiKeyScopes.Has(null, ApiKeyScopes.Read));
    }

    private static McpCaller CallerWith(string? scopes)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "7") };
        if (scopes is not null) claims.Add(new Claim(ApiKeyScopes.ClaimType, scopes));
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"))
        };
        return new McpCaller(new HttpContextAccessor { HttpContext = context });
    }

    [Fact]
    public void Read_only_key_is_refused_agent_scope()
    {
        var caller = CallerWith("read");
        Assert.Equal(7, caller.UserId);
        Assert.Throws<McpException>(() => caller.RequireScope(ApiKeyScopes.Agents));
    }

    [Fact]
    public void Agents_key_passes_agent_scope() =>
        CallerWith("read,agents").RequireScope(ApiKeyScopes.Agents);

    [Fact]
    public void Jwt_style_caller_without_scopes_has_no_access() =>
        Assert.False(CallerWith(null).HasScope(ApiKeyScopes.Read));
}
