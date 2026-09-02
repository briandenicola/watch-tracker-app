using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using WatchTracker.Api.Models;
using WatchTracker.Api.Services;

namespace WatchTracker.Api.Tests;

public class WatchImageServiceTests
{
    [Fact]
    public async Task Remote_import_rejects_content_type_without_matching_magic_bytes()
    {
        await using var database = await TestDatabase.CreateAsync();
        var (user, watch) = await AddWatchAsync(database);
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent("not an image"u8.ToArray())
        };
        response.Content.Headers.ContentType = new("image/jpeg");
        using var fixture = new ImageServiceFixture(database, response);

        var result = await fixture.Service.ImportFromUrlAsync(
            watch.Id,
            user.Id,
            "https://images.example.test/watch.jpg");

        Assert.Null(result);
        Assert.Empty(database.Context.WatchImages);
        Assert.False(Directory.Exists(fixture.UploadsPath));
    }

    [Fact]
    public async Task Remote_import_rejects_declared_oversized_response_before_downloading()
    {
        await using var database = await TestDatabase.CreateAsync();
        var (user, watch) = await AddWatchAsync(database);
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent([])
        };
        response.Content.Headers.ContentLength = 10 * 1024 * 1024 + 1;
        using var fixture = new ImageServiceFixture(database, response);

        var error = await Assert.ThrowsAsync<HttpRequestException>(() =>
            fixture.Service.ImportFromUrlAsync(
                watch.Id,
                user.Id,
                "https://images.example.test/watch.jpg"));

        Assert.Contains("too large", error.Message);
        Assert.Empty(database.Context.WatchImages);
    }

    private static async Task<(User User, Watch Watch)> AddWatchAsync(TestDatabase database)
    {
        var user = TestDatabase.User("owner");
        var watch = new Watch
        {
            Brand = "Test",
            Model = "Watch",
            IsWishList = true,
            User = user
        };
        database.Context.AddRange(user, watch);
        await database.Context.SaveChangesAsync();
        return (user, watch);
    }

    private sealed class ImageServiceFixture : IDisposable
    {
        private readonly string rootPath = Path.Combine(
            Path.GetTempPath(),
            $"watch-tracker-tests-{Guid.NewGuid():N}");

        public ImageServiceFixture(TestDatabase database, HttpResponseMessage response)
        {
            Directory.CreateDirectory(rootPath);
            var environment = new StubEnvironment(rootPath);
            var client = new HttpClient(new ResponseHandler(response));
            Service = new WatchImageService(
                database.Context,
                new UploadStorage(environment),
                new StubHttpClientFactory(client),
                new StubBackgroundRemoval());
        }

        public WatchImageService Service { get; }
        public string UploadsPath => Path.Combine(rootPath, "uploads");

        public void Dispose()
        {
            if (Directory.Exists(rootPath))
                Directory.Delete(rootPath, recursive: true);
        }
    }

    private sealed class ResponseHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            response.RequestMessage = request;
            return Task.FromResult(response);
        }
    }

    private sealed class StubHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class StubBackgroundRemoval : IBackgroundRemovalService
    {
        public bool IsAvailable => false;
        public Task<string> RemoveBackgroundAsync(
            string inputPath,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public void Dispose() { }
    }

    private sealed class StubEnvironment(string contentRootPath) : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "Tests";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = contentRootPath;
        public string EnvironmentName { get; set; } = "Test";
        public string WebRootPath { get; set; } = contentRootPath;
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }
}
