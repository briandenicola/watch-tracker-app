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
    [InlineData("read,write", "read,write")]
    [InlineData("Write", "read,write")]
    [InlineData("agents, write ,read", "read,write,agents")]
    [InlineData("wishlist", "read,wishlist")]
    [InlineData("Wishlist, agents", "read,agents,wishlist")]
    [InlineData("wishlist,agents,write,read", "read,write,agents,wishlist")]
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

    [Theory]
    [InlineData("read", false, false)]
    [InlineData("read,agents", false, true)]
    [InlineData("read,write", true, false)]
    [InlineData("read,write,agents", true, true)]
    [InlineData("read,wishlist", true, false)]
    [InlineData("read,agents,wishlist", true, true)]
    public void Tools_are_listed_only_for_keys_with_their_scope(string scopes, bool seesWrite, bool seesAgents)
    {
        var hidden = McpToolScopes.HiddenFrom(CallerWith(scopes));
        Assert.Equal(seesWrite, !hidden.Contains("add_to_wishlist"));
        Assert.Equal(seesAgents, !hidden.Contains("ask_collection_advisor"));
        Assert.DoesNotContain("list_wishlist", hidden);
    }

    [Theory]
    [InlineData("read")]
    [InlineData("read,agents")]
    public async Task Keys_without_write_cannot_add_to_the_wish_list(string scopes)
    {
        await using var db = await TestDatabase.CreateAsync();
        var tools = new WatchTrackerWriteTools(
            CallerWith(scopes), new WatchTracker.Api.Services.RecommendationWishlistService(db.Context));

        await Assert.ThrowsAsync<McpException>(() => tools.AddToWishlist("Tudor", "Black Bay 58"));
        Assert.Empty(db.Context.Watches);
    }

    [Theory]
    [InlineData("read,wishlist")]
    [InlineData("read,agents,wishlist")]
    [InlineData("read,write")]
    public async Task Wishlist_or_write_keys_can_add_to_the_wish_list(string scopes)
    {
        await using var db = await TestDatabase.CreateAsync();
        db.Context.Users.Add(new WatchTracker.Api.Models.User { Id = 7, Username = "brian", Email = "b@example.com", PasswordHash = "x" });
        await db.Context.SaveChangesAsync();
        var tools = new WatchTrackerWriteTools(
            CallerWith(scopes), new WatchTracker.Api.Services.RecommendationWishlistService(db.Context));

        await tools.AddToWishlist("Tudor", "Black Bay 58");

        Assert.Single(db.Context.Watches);
    }

    [Fact]
    public void A_wishlist_key_is_not_an_agents_key() =>
        Assert.Throws<McpException>(() => CallerWith("read,wishlist").RequireScope(ApiKeyScopes.Agents));

    [Fact]
    public void Jwt_style_caller_without_scopes_has_no_access() =>
        Assert.False(CallerWith(null).HasScope(ApiKeyScopes.Read));
}
