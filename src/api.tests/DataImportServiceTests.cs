using System.IO.Compression;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using WatchTracker.Api.Controllers;
using WatchTracker.Api.Models;
using WatchTracker.Api.Services;

namespace WatchTracker.Api.Tests;

public class DataImportServiceTests
{
    [Fact]
    public async Task Import_rejects_non_zip_files_before_creating_uploads_directory()
    {
        await using var database = await TestDatabase.CreateAsync();
        using var fixture = new ImportFixture(database);
        var file = new FormFile(new MemoryStream("not a zip"u8.ToArray()), 0, 9, "file", "collection.csv");

        var result = await fixture.Service.ImportAsync(1, file);

        Assert.Equal("Please upload a .zip file.", result.Error);
        Assert.Null(result.Result);
        Assert.False(Directory.Exists(fixture.UploadsPath));
    }

    [Fact]
    public async Task Import_maps_watches_and_wear_logs_to_the_requesting_user()
    {
        await using var database = await TestDatabase.CreateAsync();
        var owner = TestDatabase.User("owner");
        var other = TestDatabase.User("other");
        database.Context.AddRange(owner, other);
        await database.Context.SaveChangesAsync();
        using var fixture = new ImportFixture(database);
        using var stream = CreateZip("""
ExportId,Brand,Model,IsWishList,WishlistPriority,WearLogs
7,Seiko,SPB143,true,2,2026-08-01T10:00:00.0000000Z|2026-08-01T10:00:00.0000000Z|2026-08-01T18:00:00.0000000Z
""");
        var file = new FormFile(stream, 0, stream.Length, "file", "collection.ZIP");

        var result = await fixture.Service.ImportAsync(owner.Id, file);

        Assert.NotNull(result.Result);
        Assert.Equal(1, result.Result.WatchesImported);
        Assert.Equal(1, result.Result.WearLogsImported);
        var watch = Assert.Single(database.Context.Watches);
        Assert.Equal(owner.Id, watch.UserId);
        Assert.Equal("Seiko", watch.Brand);
        Assert.Equal("SPB143", watch.Model);
        Assert.True(watch.IsWishList);
        Assert.Equal(2, watch.WishlistPriority);
        var log = Assert.Single(database.Context.WearLogs);
        Assert.Equal(owner.Id, log.UserId);
        Assert.Equal(watch.Id, log.WatchId);
        Assert.NotNull(log.StartedAt);
        Assert.NotNull(log.EndedAt);
    }

    [Fact]
    public async Task Import_keeps_zip_image_paths_inside_the_uploads_directory()
    {
        await using var database = await TestDatabase.CreateAsync();
        var owner = TestDatabase.User("owner");
        database.Context.Users.Add(owner);
        await database.Context.SaveChangesAsync();
        using var fixture = new ImportFixture(database);
        using var stream = CreateZip(
            """
ExportId,Brand,Model,Images
7,Seiko,SPB143,outside.jpg
""",
            ("images/../../outside.jpg", "image-bytes"));
        var file = new FormFile(stream, 0, stream.Length, "file", "collection.zip");

        var result = await fixture.Service.ImportAsync(owner.Id, file);

        Assert.NotNull(result.Result);
        Assert.Equal(1, result.Result.ImagesImported);
        var image = Assert.Single(database.Context.WatchImages);
        Assert.EndsWith(".jpg", image.FileName);
        Assert.NotEqual("outside.jpg", image.FileName);
        Assert.DoesNotContain(
            Directory.EnumerateFiles(fixture.RootPath, "*", SearchOption.AllDirectories),
            path => Path.GetFileName(path).Equals("outside.jpg", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Export_includes_the_owners_watches_and_image_files()
    {
        await using var database = await TestDatabase.CreateAsync();
        var owner = TestDatabase.User("owner");
        var watch = new Watch
        {
            User = owner,
            Brand = "Tudor",
            Model = "Black Bay 58",
            Images = [new WatchImage { FileName = "watch.jpg", ContentType = "image/jpeg" }]
        };
        database.Context.AddRange(owner, watch);
        await database.Context.SaveChangesAsync();
        using var fixture = new ImportFixture(database);
        Directory.CreateDirectory(fixture.UploadsPath);
        await File.WriteAllBytesAsync(Path.Combine(fixture.UploadsPath, "watch.jpg"), "image-bytes"u8.ToArray());
        var controller = new DataController(database.Context, fixture.Environment, fixture.Service)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        [new Claim(ClaimTypes.NameIdentifier, owner.Id.ToString())]))
                }
            }
        };

        var result = Assert.IsType<FileStreamResult>(await controller.Export());
        await using var export = new MemoryStream();
        await result.FileStream.CopyToAsync(export);
        export.Position = 0;
        using var archive = new ZipArchive(export, ZipArchiveMode.Read);
        using var csvReader = new StreamReader(archive.GetEntry("watches.csv")!.Open());

        var csv = await csvReader.ReadToEndAsync();
        Assert.Contains("Tudor,Black Bay 58", csv);
        var image = archive.GetEntry("images/watch.jpg");
        Assert.NotNull(image);
        await using var imageStream = image.Open();
        using var imageReader = new StreamReader(imageStream);
        Assert.Equal("image-bytes", await imageReader.ReadToEndAsync());
    }

    private static MemoryStream CreateZip(string csv, params (string Path, string Content)[] files)
    {
        var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = archive.CreateEntry("watches.csv");
            using (var writer = new StreamWriter(entry.Open(), Encoding.UTF8))
            {
                writer.Write(csv);
            }
            foreach (var (path, content) in files)
            {
                var fileEntry = archive.CreateEntry(path);
                using var fileWriter = new StreamWriter(fileEntry.Open(), Encoding.UTF8);
                fileWriter.Write(content);
            }
        }
        stream.Position = 0;
        return stream;
    }

    private sealed class ImportFixture : IDisposable
    {
        private readonly string rootPath = Path.Combine(Directory.GetCurrentDirectory(), ".test-artifacts", Guid.NewGuid().ToString("N"));

        public ImportFixture(TestDatabase database)
        {
            Directory.CreateDirectory(rootPath);
            Service = new DataImportService(database.Context, new StubEnvironment(rootPath));
        }

        public DataImportService Service { get; }
        public IWebHostEnvironment Environment => new StubEnvironment(rootPath);
        public string RootPath => rootPath;
        public string UploadsPath => Path.Combine(rootPath, "uploads");

        public void Dispose()
        {
            if (Directory.Exists(rootPath)) Directory.Delete(rootPath, recursive: true);
            var parent = Directory.GetParent(rootPath)!;
            if (Directory.Exists(parent.FullName) && !Directory.EnumerateFileSystemEntries(parent.FullName).Any())
                Directory.Delete(parent.FullName);
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
