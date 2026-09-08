using Microsoft.EntityFrameworkCore;
using WatchTracker.Api.DTOs;
using WatchTracker.Api.Models;
using WatchTracker.Api.Services;

namespace WatchTracker.Api.Tests;

public class ShareServiceTests
{
    [Fact]
    public async Task Single_watch_shares_are_owner_scoped_and_redact_private_fields()
    {
        await using var database = await TestDatabase.CreateAsync();
        var owner = TestDatabase.User("owner");
        var other = TestDatabase.User("other");
        var watch = new Watch
        {
            User = owner,
            Brand = "Omega",
            Model = "Speedmaster",
            Sku = "310.30.42.50.01.001",
            PurchasePrice = 9000m,
            SerialNumber = "private-serial",
            Notes = "private notes",
            StorageLocation = "safe",
            AcquiredFrom = "private seller"
        };
        database.Context.AddRange(owner, other, watch);
        await database.Context.SaveChangesAsync();
        var service = new WatchShareService(database.Context, new AppSettingsService(database.Context));

        Assert.Null(await service.CreateAsync(watch.Id, other.Id));
        var share = await service.CreateAsync(watch.Id, owner.Id);
        var publicWatch = await service.ViewAsync(share!.Token);

        Assert.NotNull(publicWatch);
        Assert.Equal("Omega", publicWatch.Brand);
        Assert.Equal("Speedmaster", publicWatch.Model);
        Assert.Equal(watch.Sku, publicWatch.Sku);
        Assert.DoesNotContain(typeof(SharedWatchDto).GetProperties(), property =>
            property.Name is "PurchasePrice" or "SerialNumber" or "Notes" or "StorageLocation" or "AcquiredFrom" or "UserId");
        Assert.Equal(1, (await database.Context.WatchShares.SingleAsync()).ViewCount);
    }

    [Fact]
    public async Task Wishlist_share_excludes_other_owners_disposed_entries_and_prices_unless_opted_in()
    {
        await using var database = await TestDatabase.CreateAsync();
        var owner = TestDatabase.User("owner");
        var other = TestDatabase.User("other");
        var visible = new Watch { User = owner, Brand = "Seiko", Model = "SPB143", IsWishList = true, PurchasePrice = 1200m, WishlistPriority = 0 };
        var disposed = new Watch { User = owner, Brand = "Old", Model = "Wish", IsWishList = true, WishlistPriority = 1 };
        disposed.Disposition = new WatchDisposition { Type = DispositionType.Retired, DispositionDate = DateTime.UtcNow };
        var otherUsersWatch = new Watch { User = other, Brand = "Other", Model = "Wish", IsWishList = true, WishlistPriority = 0 };
        database.Context.AddRange(owner, other, visible, disposed, otherUsersWatch);
        await database.Context.SaveChangesAsync();
        var service = new WishlistShareService(database.Context, new AppSettingsService(database.Context));

        var share = await service.CreateAsync(owner.Id, new UpdateWishlistShareDto { IncludePrices = false });
        var redacted = await service.ViewAsync(share.Token);

        Assert.NotNull(redacted);
        Assert.Equal(owner.Username, redacted.OwnerName);
        var item = Assert.Single(redacted.Items);
        Assert.Equal("Seiko", item.Brand);
        Assert.Null(item.TargetPrice);
        Assert.DoesNotContain(typeof(SharedWishlistItemDto).GetProperties(), property => property.Name is "Notes" or "SerialNumber" or "StorageLocation");

        await service.UpdateAsync(owner.Id, new UpdateWishlistShareDto { IncludePrices = true });
        var withPrices = await service.ViewAsync(share.Token);
        Assert.Equal(1200m, Assert.Single(withPrices!.Items).TargetPrice);
    }

    [Fact]
    public async Task Direct_wishlist_share_is_visible_only_to_its_recipient_and_revokes_independently()
    {
        await using var database = await TestDatabase.CreateAsync();
        var owner = TestDatabase.User("owner");
        var recipient = TestDatabase.User("recipient");
        var stranger = TestDatabase.User("stranger");
        var watch = new Watch
        {
            User = owner,
            Brand = "Hamilton",
            Model = "Khaki Field",
            IsWishList = true,
            PurchasePrice = 650m,
            WishlistPriority = 0
        };
        database.Context.AddRange(owner, recipient, stranger, watch);
        await database.Context.SaveChangesAsync();
        var service = new WishlistShareService(
            database.Context,
            new AppSettingsService(database.Context));

        var direct = await service.ShareWithUserAsync(
            owner.Id,
            new CreateWishlistUserShareDto
            {
                RecipientUserId = recipient.Id,
                IncludePrices = true
            });

        Assert.NotNull(direct);
        Assert.Empty(await service.GetReceivedSharesAsync(stranger.Id));
        var received = Assert.Single(await service.GetReceivedSharesAsync(recipient.Id));
        Assert.Equal(owner.Username, received.OwnerName);
        var sharedWishlist = await service.ViewReceivedShareAsync(received.Id, recipient.Id);
        Assert.NotNull(sharedWishlist);
        Assert.Equal(650m, Assert.Single(sharedWishlist.Items).TargetPrice);
        Assert.Null(await service.ViewReceivedShareAsync(received.Id, stranger.Id));

        var publicLink = await service.CreateAsync(
            owner.Id,
            new UpdateWishlistShareDto { IncludePrices = false });
        Assert.True(await service.RevokeUserShareAsync(owner.Id, direct.Id));
        Assert.Null(await service.ViewReceivedShareAsync(received.Id, recipient.Id));
        Assert.NotNull(await service.ViewAsync(publicLink.Token));
    }

    [Fact]
    public async Task User_search_excludes_the_owner_and_requires_two_characters()
    {
        await using var database = await TestDatabase.CreateAsync();
        var owner = TestDatabase.User("owner");
        var match = TestDatabase.User("another-owner");
        database.Context.Users.AddRange(owner, match);
        await database.Context.SaveChangesAsync();
        var service = new WishlistShareService(
            database.Context,
            new AppSettingsService(database.Context));

        Assert.Empty(await service.SearchUsersAsync(owner.Id, "o"));
        var result = Assert.Single(await service.SearchUsersAsync(owner.Id, "owner"));
        Assert.Equal(match.Id, result.Id);
    }
}
