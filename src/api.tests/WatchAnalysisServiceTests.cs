using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using WatchTracker.Api.Models;
using WatchTracker.Api.Services;

namespace WatchTracker.Api.Tests;

public class WatchAnalysisServiceTests
{
    [Fact]
    public async Task Analysis_reencodes_imported_webp_as_bounded_jpeg_for_ollama()
    {
        await using var database = await TestDatabase.CreateAsync();
        using var fixture = new AnalysisFixture();
        var user = TestDatabase.User("owner");
        var watch = new Watch
        {
            Brand = "Test",
            Model = "Wishlist Watch",
            IsWishList = true,
            User = user
        };
        var storedImage = new WatchImage
        {
            Watch = watch,
            FileName = "cover.webp",
            ContentType = "image/webp",
            SortOrder = 0
        };
        database.Context.AddRange(user, watch, storedImage);
        await database.Context.SaveChangesAsync();

        using (var source = new Image<Rgba32>(1800, 900, new Rgba32(0, 0, 0, 0)))
            await source.SaveAsWebpAsync(Path.Combine(fixture.UploadsPath, storedImage.FileName));

        var handler = new CapturingOllamaHandler();
        var service = new WatchAnalysisService(
            database.Context,
            new StubSettings(),
            new WatchCatalogService(
                database.Context,
                new UploadStorage(fixture.Environment),
                NullLogger<WatchCatalogService>.Instance),
            new StubPageReader(),
            new HttpClient(handler),
            new UploadStorage(fixture.Environment),
            NullLogger<WatchAnalysisService>.Instance);

        var result = await service.AnalyzeAsync(watch.Id, user.Id);

        Assert.Equal("Normalized image.", result.Summary);
        Assert.Equal("JPEG", handler.ImageFormat);
        Assert.Equal(1024, handler.Width);
        Assert.Equal(512, handler.Height);
    }

    private sealed class CapturingOllamaHandler : HttpMessageHandler
    {
        public string? ImageFormat { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var body = await request.Content!.ReadAsStringAsync(cancellationToken);
            using var json = JsonDocument.Parse(body);
            var base64 = json.RootElement
                .GetProperty("messages")[0]
                .GetProperty("images")[0]
                .GetString()!;
            var bytes = Convert.FromBase64String(base64);
            ImageFormat = Image.DetectFormat(bytes).Name;
            using var image = Image.Load<Rgba32>(bytes);
            Width = image.Width;
            Height = image.Height;

            var response = JsonSerializer.Serialize(new
            {
                message = new
                {
                    content = """{"summary":"Normalized image.","suggestions":[]}"""
                }
            });
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(response)
            };
        }
    }

    private sealed class StubPageReader : IProductPageReader
    {
        public Task<LinkedPageExcerpt?> ReadAsync(
            string url,
            CancellationToken ct = default) =>
            Task.FromResult<LinkedPageExcerpt?>(null);
    }

    private sealed class StubSettings : IAppSettingsService
    {
        public Task<string> GetAsync(string key, string defaultValue = "") =>
            Task.FromResult(key == AppSettingsService.Keys.OllamaModel
                ? "vision-model"
                : "http://ollama.test");

        public Task<int> GetIntAsync(string key, int defaultValue) =>
            Task.FromResult(defaultValue);

        public Task SetAsync(string key, string value) => Task.CompletedTask;

        public Task<Dictionary<string, string>> GetAllAsync() =>
            Task.FromResult(new Dictionary<string, string>());
    }

    private sealed class AnalysisFixture : IDisposable
    {
        private readonly string rootPath = Path.Combine(
            Path.GetTempPath(),
            $"watch-analysis-tests-{Guid.NewGuid():N}");

        public AnalysisFixture()
        {
            UploadsPath = Path.Combine(rootPath, "uploads");
            Directory.CreateDirectory(UploadsPath);
            Environment = new StubEnvironment(rootPath);
        }

        public string UploadsPath { get; }
        public IWebHostEnvironment Environment { get; }

        public void Dispose()
        {
            if (Directory.Exists(rootPath))
                Directory.Delete(rootPath, recursive: true);
        }
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
