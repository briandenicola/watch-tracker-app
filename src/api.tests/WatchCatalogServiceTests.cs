using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using WatchTracker.Api.Models;
using WatchTracker.Api.Services;

namespace WatchTracker.Api.Tests;

public class WatchCatalogServiceTests
{
    [Fact]
    public async Task Deleting_a_watch_removes_its_image_files_from_disk()
    {
        await using var database = await TestDatabase.CreateAsync();
        using var fixture = new CatalogFixture();
        var owner = TestDatabase.User("owner");
        database.Context.Users.Add(owner);
        await database.Context.SaveChangesAsync();

        var watch = new Watch
        {
            UserId = owner.Id,
            Brand = "Tudor",
            Model = "Black Bay 58",
            Images =
            [
                new WatchImage { FileName = $"{owner.Id}/cover.jpg", ContentType = "image/jpeg" },
                new WatchImage { FileName = $"{owner.Id}/side.png", ContentType = "image/png" }
            ]
        };
        database.Context.Watches.Add(watch);
        await database.Context.SaveChangesAsync();

        var userDir = fixture.Storage.EnsureUserDirectory(owner.Id);
        var cover = Path.Combine(userDir, "cover.jpg");
        var side = Path.Combine(userDir, "side.png");
        await File.WriteAllTextAsync(cover, "cover");
        await File.WriteAllTextAsync(side, "side");

        var deleted = await fixture.Service(database).DeleteAsync(watch.Id, owner.Id);

        Assert.True(deleted);
        Assert.False(File.Exists(cover));
        Assert.False(File.Exists(side));
        Assert.Empty(database.Context.WatchImages);
        Assert.Empty(database.Context.Watches);
    }

    [Fact]
    public async Task Deleting_a_watch_removes_an_image_still_at_the_uploads_root()
    {
        await using var database = await TestDatabase.CreateAsync();
        using var fixture = new CatalogFixture();
        var owner = TestDatabase.User("owner");
        database.Context.Users.Add(owner);
        await database.Context.SaveChangesAsync();

        // Written before uploads moved into per-user directories.
        var watch = new Watch
        {
            UserId = owner.Id,
            Brand = "Seiko",
            Model = "SPB143",
            Images = [new WatchImage { FileName = "legacy.jpg", ContentType = "image/jpeg" }]
        };
        database.Context.Watches.Add(watch);
        await database.Context.SaveChangesAsync();

        Directory.CreateDirectory(fixture.UploadsPath);
        var legacy = Path.Combine(fixture.UploadsPath, "legacy.jpg");
        await File.WriteAllTextAsync(legacy, "legacy");

        Assert.True(await fixture.Service(database).DeleteAsync(watch.Id, owner.Id));
        Assert.False(File.Exists(legacy));
    }

    [Fact]
    public async Task Deleting_a_watch_whose_file_is_already_gone_still_succeeds()
    {
        await using var database = await TestDatabase.CreateAsync();
        using var fixture = new CatalogFixture();
        var owner = TestDatabase.User("owner");
        database.Context.Users.Add(owner);
        await database.Context.SaveChangesAsync();

        var watch = new Watch
        {
            UserId = owner.Id,
            Brand = "Seiko",
            Model = "SPB143",
            Images = [new WatchImage { FileName = $"{owner.Id}/absent.jpg", ContentType = "image/jpeg" }]
        };
        database.Context.Watches.Add(watch);
        await database.Context.SaveChangesAsync();

        Assert.True(await fixture.Service(database).DeleteAsync(watch.Id, owner.Id));
        Assert.Empty(database.Context.Watches);
    }

    [Fact]
    public async Task Another_users_watch_is_neither_deleted_nor_has_its_files_touched()
    {
        await using var database = await TestDatabase.CreateAsync();
        using var fixture = new CatalogFixture();
        var owner = TestDatabase.User("owner");
        var stranger = TestDatabase.User("stranger");
        database.Context.Users.AddRange(owner, stranger);
        await database.Context.SaveChangesAsync();

        var watch = new Watch
        {
            UserId = owner.Id,
            Brand = "Tudor",
            Model = "Pelagos",
            Images = [new WatchImage { FileName = $"{owner.Id}/cover.jpg", ContentType = "image/jpeg" }]
        };
        database.Context.Watches.Add(watch);
        await database.Context.SaveChangesAsync();

        var cover = Path.Combine(fixture.Storage.EnsureUserDirectory(owner.Id), "cover.jpg");
        await File.WriteAllTextAsync(cover, "cover");

        var deleted = await fixture.Service(database).DeleteAsync(watch.Id, stranger.Id);

        Assert.False(deleted);
        Assert.True(File.Exists(cover));
        Assert.Single(database.Context.Watches);
    }

    private sealed class CatalogFixture : IDisposable
    {
        private readonly string rootPath = Path.Combine(
            Path.GetTempPath(),
            $"watch-catalog-tests-{Guid.NewGuid():N}");

        public CatalogFixture()
        {
            Directory.CreateDirectory(rootPath);
            Storage = new UploadStorage(new StubEnvironment(rootPath));
        }

        public UploadStorage Storage { get; }
        public string UploadsPath => Path.Combine(rootPath, "uploads");

        public WatchCatalogService Service(TestDatabase database) => new(
            database.Context,
            Storage,
            NullLogger<WatchCatalogService>.Instance);

        public void Dispose()
        {
            if (Directory.Exists(rootPath))
                Directory.Delete(rootPath, recursive: true);
        }
    }

    private sealed class StubEnvironment(string contentRootPath) : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "WatchTracker.Api.Tests";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = contentRootPath;
        public string EnvironmentName { get; set; } = "Test";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = contentRootPath;
    }
}
