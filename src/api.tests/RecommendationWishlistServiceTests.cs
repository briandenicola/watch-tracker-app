using Microsoft.EntityFrameworkCore;
using WatchTracker.Api.DTOs;
using WatchTracker.Api.Models;
using WatchTracker.Api.Services;

namespace WatchTracker.Api.Tests;

public class RecommendationWishlistServiceTests
{
    [Fact]
    public async Task A_card_becomes_a_wish_list_watch_without_any_advisor_involved()
    {
        await using var database = await TestDatabase.CreateAsync();
        var user = await AddUserAsync(database);
        var service = new RecommendationWishlistService(database.Context);

        var result = await service.AddAsync(Card(), user.Id, "Added from a collection review.");

        Assert.True(result!.Added);
        var watch = await database.Context.Watches.SingleAsync();
        Assert.Equal("Hamilton", watch.Brand);
        Assert.Equal("Khaki Field", watch.Model);
        Assert.True(watch.IsWishList);
        Assert.Equal(0, watch.WishlistPriority);
        Assert.Equal(995m, watch.PurchasePrice);
        // The caller says where the card came from, so a second source does not
        // inherit the advisor's wording.
        Assert.Equal("Added from a collection review.", watch.Notes);
    }

    [Fact]
    public async Task Shipping_is_included_in_the_recorded_price_when_the_card_carries_a_total()
    {
        await using var database = await TestDatabase.CreateAsync();
        var user = await AddUserAsync(database);
        var service = new RecommendationWishlistService(database.Context);
        var card = Card();
        card.TotalPrice = 1020m;

        await service.AddAsync(card, user.Id, "note");

        var watch = await database.Context.Watches.SingleAsync();
        Assert.Equal(1020m, watch.PurchasePrice);
    }

    [Fact]
    public async Task A_card_with_no_link_is_refused()
    {
        await using var database = await TestDatabase.CreateAsync();
        var user = await AddUserAsync(database);
        var service = new RecommendationWishlistService(database.Context);
        var card = Card();
        card.ItemUrl = null;

        var result = await service.AddAsync(card, user.Id, "note");

        Assert.Null(result);
        Assert.Empty(await database.Context.Watches.ToListAsync());
    }

    [Fact]
    public async Task Two_adds_of_the_same_card_at_once_still_produce_one_watch()
    {
        await using var database = await TestDatabase.CreateAsync();
        var user = await AddUserAsync(database);
        var service = new RecommendationWishlistService(database.Context);

        // Both start on their own thread and wait on the same signal, so they
        // genuinely overlap rather than the first finishing before the second is
        // asked for.
        var start = new TaskCompletionSource();
        var first = Task.Run(async () =>
        {
            await start.Task;
            return await service.AddAsync(Card(), user.Id, "note");
        });
        var second = Task.Run(async () =>
        {
            await start.Task;
            return await service.AddAsync(Card(), user.Id, "note");
        });
        start.SetResult();

        var results = await Task.WhenAll(first, second);

        var watch = await database.Context.Watches.SingleAsync();
        Assert.Single(results, r => r!.Added);
        Assert.Single(results, r => !r!.Added);
        Assert.All(results, r => Assert.Equal(watch.Id, r!.WatchId));
    }

    [Fact]
    public async Task A_disposed_wish_list_watch_does_not_block_the_card()
    {
        await using var database = await TestDatabase.CreateAsync();
        var user = await AddUserAsync(database);
        var service = new RecommendationWishlistService(database.Context);
        // The same watch, recorded by hand and since disposed of. Duplicate
        // detection only looks at what is still on the wish list.
        var disposed = new Watch
        {
            UserId = user.Id,
            Brand = "Hamilton",
            Model = "Khaki Field",
            Sku = "H70455533",
            MovementType = MovementType.Unknown,
            IsWishList = true
        };
        database.Context.Watches.Add(disposed);
        await database.Context.SaveChangesAsync();
        database.Context.WatchDispositions.Add(new WatchDisposition
        {
            WatchId = disposed.Id,
            Type = DispositionType.Sold,
            DispositionDate = DateTime.UtcNow
        });
        await database.Context.SaveChangesAsync();

        var result = await service.AddAsync(Card(), user.Id, "note");

        Assert.True(result!.Added);
        Assert.NotEqual(disposed.Id, result.WatchId);
    }

    [Fact]
    public async Task A_listing_already_bought_and_sold_is_reported_not_inserted_twice()
    {
        await using var database = await TestDatabase.CreateAsync();
        var user = await AddUserAsync(database);
        var service = new RecommendationWishlistService(database.Context);
        // Wanted, bought, worn, sold — the row keeps the listing it came from, and
        // the database allows only one row per user per listing.
        var bought = new Watch
        {
            UserId = user.Id,
            Brand = "Hamilton",
            Model = "Khaki Field",
            MovementType = MovementType.Unknown,
            IsWishList = false,
            MarketplaceProvider = "eBay",
            MarketplaceItemId = "item-1"
        };
        database.Context.Watches.Add(bought);
        await database.Context.SaveChangesAsync();
        database.Context.WatchDispositions.Add(new WatchDisposition
        {
            WatchId = bought.Id,
            Type = DispositionType.Sold,
            DispositionDate = DateTime.UtcNow
        });
        await database.Context.SaveChangesAsync();

        var result = await service.AddAsync(Card(), user.Id, "note");

        Assert.False(result!.Added);
        Assert.Equal(bought.Id, result.WatchId);
        Assert.Equal("You already recorded this listing on another watch.", result.Message);
        Assert.Single(await database.Context.Watches.ToListAsync());
    }

    [Fact]
    public async Task A_listing_on_an_owned_watch_is_reported_as_such_not_as_a_wish_list_duplicate()
    {
        await using var database = await TestDatabase.CreateAsync();
        var user = await AddUserAsync(database);
        var service = new RecommendationWishlistService(database.Context);
        database.Context.Watches.Add(new Watch
        {
            UserId = user.Id,
            Brand = "Hamilton",
            Model = "Khaki Field",
            MovementType = MovementType.Unknown,
            IsWishList = false,
            MarketplaceProvider = "eBay",
            MarketplaceItemId = "item-1"
        });
        await database.Context.SaveChangesAsync();

        var result = await service.AddAsync(Card(), user.Id, "note");

        Assert.False(result!.Added);
        Assert.Equal("You already recorded this listing on another watch.", result.Message);
    }

    private static AdvisorRecommendationCardDto Card() => new()
    {
        Provider = "eBay",
        ProviderItemId = "item-1",
        Title = "Hamilton Khaki Field",
        Brand = "Hamilton",
        Model = "Khaki Field",
        ReferenceNumber = "H70455533",
        ItemUrl = "https://example.test/item-1",
        Price = 995m,
        Currency = "USD",
        ObservedAt = DateTime.UtcNow
    };

    private static async Task<User> AddUserAsync(TestDatabase database)
    {
        var user = TestDatabase.User("owner");
        database.Context.Users.Add(user);
        await database.Context.SaveChangesAsync();
        return user;
    }
}
