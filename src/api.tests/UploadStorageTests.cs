using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using WatchTracker.Api.Models;
using WatchTracker.Api.Services;

namespace WatchTracker.Api.Tests;

public class UploadStorageTests
{
    [Fact]
    public void Stored_name_places_the_file_in_the_owners_directory()
    {
        using var fixture = new StorageFixture();

        Assert.Equal("7/watch.jpg", fixture.Storage.StoredName(7, "watch.jpg"));
        Assert.Equal(
            Path.Combine(fixture.UploadsPath, "7"),
            fixture.Storage.EnsureUserDirectory(7));
        Assert.True(Directory.Exists(Path.Combine(fixture.UploadsPath, "7")));
    }

    [Fact]
    public void Resolves_a_file_inside_the_owners_directory()
    {
        using var fixture = new StorageFixture();
        var expected = Path.Combine(fixture.Storage.EnsureUserDirectory(7), "watch.jpg");
        File.WriteAllText(expected, "image");

        Assert.True(fixture.Storage.TryGetFilePath("7/watch.jpg", out var path));
        Assert.Equal(expected, path);
    }

    [Fact]
    public void Falls_back_to_the_uploads_root_for_files_not_yet_moved()
    {
        using var fixture = new StorageFixture();
        Directory.CreateDirectory(fixture.UploadsPath);
        var legacy = Path.Combine(fixture.UploadsPath, "watch.jpg");
        File.WriteAllText(legacy, "image");

        Assert.True(fixture.Storage.TryGetFilePath("7/watch.jpg", out var path));
        Assert.Equal(legacy, path);
        Assert.True(fixture.Storage.TryGetFilePath("watch.jpg", out var bare));
        Assert.Equal(legacy, bare);
    }

    [Fact]
    public void Refuses_names_that_climb_out_of_the_uploads_directory()
    {
        using var fixture = new StorageFixture();
        var outside = Path.Combine(fixture.RootPath, "secret.txt");
        File.WriteAllText(outside, "secret");

        Assert.False(fixture.Storage.TryGetFilePath("../secret.txt", out _));
        Assert.False(fixture.Storage.TryGetFilePath(outside, out _));
    }

    [Fact]
    public void Reports_missing_files()
    {
        using var fixture = new StorageFixture();

        Assert.False(fixture.Storage.TryGetFilePath("7/absent.jpg", out _));
        Assert.False(fixture.Storage.TryGetFilePath("", out _));
    }

    [Fact]
    public async Task Migrator_moves_existing_uploads_into_per_user_directories()
    {
        await using var database = await TestDatabase.CreateAsync();
        using var fixture = new StorageFixture();
        var owner = TestDatabase.User("owner");
        owner.ProfileImage = "profile-owner.png";
        var watch = new Watch
        {
            User = owner,
            Brand = "Tudor",
            Model = "Black Bay 58",
            Images = [new WatchImage { FileName = "watch.jpg", ContentType = "image/jpeg" }]
        };
        database.Context.AddRange(owner, watch);
        await database.Context.SaveChangesAsync();

        Directory.CreateDirectory(fixture.UploadsPath);
        await File.WriteAllTextAsync(Path.Combine(fixture.UploadsPath, "watch.jpg"), "image");
        await File.WriteAllTextAsync(Path.Combine(fixture.UploadsPath, "profile-owner.png"), "avatar");

        await fixture.Migrator(database).MigrateAsync();

        var image = Assert.Single(database.Context.WatchImages);
        Assert.Equal($"{owner.Id}/watch.jpg", image.FileName);
        Assert.Equal($"{owner.Id}/profile-owner.png", owner.ProfileImage);
        var userDir = Path.Combine(fixture.UploadsPath, owner.Id.ToString());
        Assert.True(File.Exists(Path.Combine(userDir, "watch.jpg")));
        Assert.True(File.Exists(Path.Combine(userDir, "profile-owner.png")));
        Assert.False(File.Exists(Path.Combine(fixture.UploadsPath, "watch.jpg")));
        Assert.False(File.Exists(Path.Combine(fixture.UploadsPath, "profile-owner.png")));
    }

    [Fact]
    public async Task Migrator_leaves_records_that_are_already_per_user_alone()
    {
        await using var database = await TestDatabase.CreateAsync();
        using var fixture = new StorageFixture();
        var owner = TestDatabase.User("owner");
        var watch = new Watch
        {
            User = owner,
            Brand = "Seiko",
            Model = "SPB143",
            Images = [new WatchImage { FileName = "1/watch.jpg", ContentType = "image/jpeg" }]
        };
        database.Context.AddRange(owner, watch);
        await database.Context.SaveChangesAsync();
        Directory.CreateDirectory(fixture.UploadsPath);

        await fixture.Migrator(database).MigrateAsync();

        Assert.Equal("1/watch.jpg", Assert.Single(database.Context.WatchImages).FileName);
    }

    [Fact]
    public async Task Migrator_keeps_the_record_when_the_file_is_gone()
    {
        await using var database = await TestDatabase.CreateAsync();
        using var fixture = new StorageFixture();
        var owner = TestDatabase.User("owner");
        var watch = new Watch
        {
            User = owner,
            Brand = "Seiko",
            Model = "SPB143",
            Images = [new WatchImage { FileName = "absent.jpg", ContentType = "image/jpeg" }]
        };
        database.Context.AddRange(owner, watch);
        await database.Context.SaveChangesAsync();
        Directory.CreateDirectory(fixture.UploadsPath);

        await fixture.Migrator(database).MigrateAsync();

        Assert.Equal("absent.jpg", Assert.Single(database.Context.WatchImages).FileName);
    }

    private sealed class StorageFixture : IDisposable
    {
        public StorageFixture()
        {
            RootPath = Path.Combine(
                Path.GetTempPath(),
                $"watch-tracker-storage-tests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(RootPath);
            Storage = new UploadStorage(new StubEnvironment(RootPath));
        }

        public string RootPath { get; }
        public UploadStorage Storage { get; }
        public string UploadsPath => Path.Combine(RootPath, "uploads");

        public UploadLayoutMigrator Migrator(TestDatabase database) => new(
            database.Context,
            Storage,
            NullLogger<UploadLayoutMigrator>.Instance);

        public void Dispose()
        {
            if (Directory.Exists(RootPath))
                Directory.Delete(RootPath, recursive: true);
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
