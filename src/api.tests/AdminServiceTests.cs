using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using WatchTracker.Api.Models;
using WatchTracker.Api.Services;

namespace WatchTracker.Api.Tests;

public class AdminServiceTests
{
    [Fact]
    public async Task Deleting_a_user_removes_their_records_and_uploaded_files()
    {
        await using var database = await TestDatabase.CreateAsync();
        using var fixture = new AdminFixture();
        var admin = TestDatabase.User("admin");
        admin.Role = UserRole.Admin;
        var target = TestDatabase.User("target");
        database.Context.Users.AddRange(admin, target);
        await database.Context.SaveChangesAsync();

        target.ProfileImage = fixture.Storage.StoredName(target.Id, "profile.jpg");
        var watch = new Watch
        {
            UserId = target.Id,
            Brand = "Tudor",
            Model = "Black Bay",
            Images =
            [
                new WatchImage
                {
                    FileName = fixture.Storage.StoredName(target.Id, "watch.jpg"),
                    ContentType = "image/jpeg"
                }
            ]
        };
        database.Context.Watches.Add(watch);
        await database.Context.SaveChangesAsync();

        var userDirectory = fixture.Storage.EnsureUserDirectory(target.Id);
        var profilePath = Path.Combine(userDirectory, "profile.jpg");
        var watchPath = Path.Combine(userDirectory, "watch.jpg");
        await File.WriteAllTextAsync(profilePath, "profile");
        await File.WriteAllTextAsync(watchPath, "watch");

        var result = await fixture.Service(database).DeleteUserAsync(target.Id, admin.Id);

        Assert.Equal(DeleteUserResult.Deleted, result);
        Assert.DoesNotContain(database.Context.Users, user => user.Id == target.Id);
        Assert.Empty(database.Context.Watches);
        Assert.Empty(database.Context.WatchImages);
        Assert.False(File.Exists(profilePath));
        Assert.False(File.Exists(watchPath));
    }

    [Fact]
    public async Task An_admin_cannot_delete_their_own_account()
    {
        await using var database = await TestDatabase.CreateAsync();
        using var fixture = new AdminFixture();
        var admin = TestDatabase.User("admin");
        admin.Role = UserRole.Admin;
        database.Context.Users.Add(admin);
        await database.Context.SaveChangesAsync();

        var result = await fixture.Service(database).DeleteUserAsync(admin.Id, admin.Id);

        Assert.Equal(DeleteUserResult.CannotDeleteSelf, result);
        Assert.Contains(database.Context.Users, user => user.Id == admin.Id);
    }

    [Fact]
    public async Task Listing_users_marks_the_requesting_admin()
    {
        await using var database = await TestDatabase.CreateAsync();
        using var fixture = new AdminFixture();
        var admin = TestDatabase.User("admin");
        var other = TestDatabase.User("other");
        database.Context.Users.AddRange(admin, other);
        await database.Context.SaveChangesAsync();

        var users = await fixture.Service(database).ListUsersAsync(admin.Id);

        Assert.True(users.Single(user => user.Id == admin.Id).IsCurrentUser);
        Assert.False(users.Single(user => user.Id == other.Id).IsCurrentUser);
    }

    private sealed class AdminFixture : IDisposable
    {
        private readonly string root = Path.Combine(
            Path.GetTempPath(),
            "watch-tracker-admin-tests",
            Guid.NewGuid().ToString("N"));

        public AdminFixture()
        {
            Directory.CreateDirectory(root);
            Storage = new UploadStorage(new TestEnvironment(root));
        }

        public UploadStorage Storage { get; }

        public AdminService Service(TestDatabase database) =>
            new(database.Context, Storage, NullLogger<AdminService>.Instance);

        public void Dispose()
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    private sealed class TestEnvironment(string contentRootPath) : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "WatchTracker.Api.Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = contentRootPath;
        public string EnvironmentName { get; set; } = "Testing";
        public string ContentRootPath { get; set; } = contentRootPath;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
