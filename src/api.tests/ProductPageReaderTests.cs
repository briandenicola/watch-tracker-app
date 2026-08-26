using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using WatchTracker.Api.Services;

namespace WatchTracker.Api.Tests;

public class ProductPageReaderTests
{
    [Fact]
    public async Task Safe_handler_refuses_loopback_at_connect_time()
    {
        using var handler = ProductPageReader.CreateHandler();
        using var client = new HttpClient(handler);

        var error = await Assert.ThrowsAsync<HttpRequestException>(() =>
            client.GetAsync("http://localhost:80"));

        Assert.Contains("does not resolve to a public address", error.ToString());
    }

    [Fact]
    public async Task Read_preserves_bounded_product_metadata_before_stripping_scripts()
    {
        const string html = """
            <html>
              <head>
                <title>Test Watch</title>
                <meta content="Example Store" property="og:site_name">
                <meta property="og:image" content="/images/watch.jpg">
                <script type="application/ld+json">
                  {"@type":"Product","brand":{"name":"Seiko"},"model":"SPB143"}
                </script>
              </head>
              <body><h1>Seiko Prospex</h1></body>
            </html>
            """;
        var client = new HttpClient(new StaticHandler(html));
        var reader = new ProductPageReader(
            client,
            NullLogger<ProductPageReader>.Instance);

        var result = await reader.ReadAsync("https://shop.example.test/watch");

        Assert.NotNull(result);
        Assert.Equal("Test Watch", result.Title);
        Assert.Equal("Example Store", result.Metadata!["og:site_name"]);
        Assert.Equal(
            "https://shop.example.test/images/watch.jpg",
            result.Metadata["og:image"]);
        Assert.Contains("\"@type\":\"Product\"", Assert.Single(result.JsonLd!));
        Assert.DoesNotContain("@type", result.Text);
    }

    private sealed class StaticHandler(string html) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                RequestMessage = request,
                Content = new StringContent(html)
                {
                    Headers = { ContentType = new("text/html") }
                }
            });
    }
}
