using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol;
using WatchTracker.Api.Authentication;
using WatchTracker.Api.Mcp;
using WatchTracker.Api.Models;
using WatchTracker.Api.Services;

namespace WatchTracker.Api.Tests;

// add_to_wishlist is the one MCP tool that changes a collector's data.
public class McpAddToWishlistTests
{
    [Fact]
    public async Task A_named_watch_lands_at_the_end_of_the_callers_wish_list()
    {
        await using var db = await TestDatabase.CreateAsync();
        var user = await AddUserAsync(db, "brian");
        db.Context.Watches.Add(new Watch
        {
            UserId = user.Id, Brand = "Omega", Model = "Speedmaster",
            MovementType = MovementType.Manual, IsWishList = true, WishlistPriority = 4
        });
        await db.Context.SaveChangesAsync();

        var result = await Tools(db, user).AddToWishlist(
            " Tudor ", "Black Bay 58", referenceNumber: "M79030N", linkUrl: "https://example.test/bb58",
            price: 3800m, currency: "usd", notes: "Wants a smaller diver.");

        Assert.True(Read<bool>(result, "added"));
        var watch = await db.Context.Watches.SingleAsync(w => w.Brand == "Tudor");
        Assert.Equal(watch.Id, Read<int>(result, "watchId"));
        Assert.Equal(user.Id, watch.UserId);
        Assert.Equal("Black Bay 58", watch.Model);
        Assert.True(watch.IsWishList);
        Assert.Equal(5, watch.WishlistPriority);
        Assert.Equal("M79030N", watch.Sku);
        Assert.Equal("https://example.test/bb58", watch.LinkUrl);
        Assert.Equal(3800m, watch.PurchasePrice);
        Assert.Equal("USD", watch.MarketplaceCurrency);
        Assert.Equal("Wants a smaller diver.", watch.Notes);
    }

    [Fact]
    public async Task Brand_and_model_alone_are_enough()
    {
        await using var db = await TestDatabase.CreateAsync();
        var user = await AddUserAsync(db, "brian");

        var result = await Tools(db, user).AddToWishlist("Tudor", "Black Bay 58");

        Assert.True(Read<bool>(result, "added"));
        var watch = await db.Context.Watches.SingleAsync();
        Assert.Null(watch.LinkUrl);
        Assert.Null(watch.LinkText);
        Assert.Equal(WatchTrackerWriteTools.DefaultNote, watch.Notes);
    }

    [Fact]
    public async Task A_watch_already_on_the_wish_list_is_reported_not_added_twice()
    {
        await using var db = await TestDatabase.CreateAsync();
        var user = await AddUserAsync(db, "brian");
        var tools = Tools(db, user);
        var first = await tools.AddToWishlist("Tudor", "Black Bay 58");

        var second = await tools.AddToWishlist("tudor", "Black-Bay 58");

        Assert.False(Read<bool>(second, "added"));
        Assert.Equal(Read<int>(first, "watchId"), Read<int>(second, "watchId"));
        Assert.Single(await db.Context.Watches.ToListAsync());
    }

    // Before this tool every add carried a link; two linkless watches must not
    // be taken for each other just because neither has one.
    [Fact]
    public async Task Two_different_watches_without_links_are_both_added()
    {
        await using var db = await TestDatabase.CreateAsync();
        var user = await AddUserAsync(db, "brian");
        var tools = Tools(db, user);

        await tools.AddToWishlist("Tudor", "Black Bay 58");
        var second = await tools.AddToWishlist("Seiko", "SPB143");

        Assert.True(Read<bool>(second, "added"));
        Assert.Equal(2, await db.Context.Watches.CountAsync());
    }

    [Fact]
    public async Task Another_users_wish_list_is_neither_read_nor_changed()
    {
        await using var db = await TestDatabase.CreateAsync();
        var brian = await AddUserAsync(db, "brian");
        var nelie = await AddUserAsync(db, "nelie");
        await Tools(db, nelie).AddToWishlist("Tudor", "Black Bay 58");

        var result = await Tools(db, brian).AddToWishlist("Tudor", "Black Bay 58");

        Assert.True(Read<bool>(result, "added"));
        Assert.Equal(1, await db.Context.Watches.CountAsync(w => w.UserId == brian.Id));
        Assert.Equal(1, await db.Context.Watches.CountAsync(w => w.UserId == nelie.Id));
    }

    [Theory]
    [InlineData("", "Black Bay 58", null, null, null)]
    [InlineData("Tudor", "   ", null, null, null)]
    [InlineData("Tudor", "Black Bay 58", "javascript:alert(1)", null, null)]
    [InlineData("Tudor", "Black Bay 58", "/relative", null, null)]
    [InlineData("Tudor", "Black Bay 58", null, -1, null)]
    [InlineData("Tudor", "Black Bay 58", null, 100, "dollars")]
    public async Task Bad_arguments_are_refused_and_nothing_is_written(
        string brand, string model, string? linkUrl, int? price, string? currency)
    {
        await using var db = await TestDatabase.CreateAsync();
        var user = await AddUserAsync(db, "brian");

        await Assert.ThrowsAsync<McpException>(() => Tools(db, user).AddToWishlist(
            brand, model, linkUrl: linkUrl, price: price, currency: currency));
        Assert.Empty(await db.Context.Watches.ToListAsync());
    }

    private static WatchTrackerWriteTools Tools(TestDatabase db, User user)
    {
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ApiKeyScopes.ClaimType, "read,write")
                ],
                "test"))
        };
        return new WatchTrackerWriteTools(
            new McpCaller(new HttpContextAccessor { HttpContext = context }),
            new RecommendationWishlistService(db.Context));
    }

    private static T Read<T>(object result, string property) =>
        JsonSerializer.SerializeToElement(result).GetProperty(property).Deserialize<T>()!;

    private static async Task<User> AddUserAsync(TestDatabase db, string name)
    {
        var user = new User { Username = name, Email = $"{name}@example.com", PasswordHash = "x" };
        db.Context.Users.Add(user);
        await db.Context.SaveChangesAsync();
        return user;
    }
}
