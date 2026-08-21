using System.Text.Json;

namespace WatchTracker.Api.Services;

public class SearXngSearchClient(
    HttpClient httpClient,
    IAppSettingsService appSettings,
    ILogger<SearXngSearchClient> logger) : IWebSearchClient, ISearXngTestClient
{
    public string ProviderName => "SearXNG";

    public async Task<(bool Success, string Message)> TestConnectionAsync(string url, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(url))
            return (false, "URL is required.");

        var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{url.TrimEnd('/')}/search?q=test&format=json");
        request.Headers.Add("Accept", "application/json");

        try
        {
            var response = await httpClient.SendAsync(request, ct);
            var body = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
                return (false, $"SearXNG returned {(int)response.StatusCode} {response.StatusCode}.");

            using var doc = JsonDocument.Parse(body);
            if (!doc.RootElement.TryGetProperty("results", out var results))
                return (false, "Connected, but the response didn't include a 'results' field — confirm the JSON output format is enabled on this SearXNG instance (search.formats in settings.yml).");

            return (true, $"Connected — {results.GetArrayLength()} result(s) for a test query.");
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (JsonException)
        {
            return (false, "Connected, but the response wasn't valid JSON — confirm the JSON output format is enabled on this SearXNG instance.");
        }
        catch (Exception ex)
        {
            return (false, $"Could not connect: {ex.Message}");
        }
    }

    public async Task<WebSearchResult> SearchAsync(string query, CancellationToken ct = default)
    {
        var baseUrl = await appSettings.GetAsync(AppSettingsService.Keys.SearXngUrl);
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            logger.LogInformation("SearXNG URL is not configured; skipping SearXNG search.");
            return new WebSearchResult(
                WebSearchStatus.NotConfigured,
                [],
                "SearXNG is not configured.");
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{baseUrl.TrimEnd('/')}/search?q={Uri.EscapeDataString(query)}&format=json");
        request.Headers.Add("Accept", "application/json");

        try
        {
            using var response = await httpClient.SendAsync(request, ct);
            var body = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("SearXNG API error {Status}: {Body}", response.StatusCode, body);
                return new WebSearchResult(
                    WebSearchStatus.ProviderError,
                    [],
                    $"SearXNG returned HTTP {(int)response.StatusCode}.");
            }

            using var doc = JsonDocument.Parse(body);
            if (!doc.RootElement.TryGetProperty("results", out var results))
                return new WebSearchResult(WebSearchStatus.Success, []);

            var observedAt = DateTime.UtcNow;
            var items = results.EnumerateArray()
                .Take(10)
                .Select(r => CreateResultItem(r, observedAt))
                .Where(r => r is not null)
                .Select(r => r!)
                .ToList();
            return new WebSearchResult(WebSearchStatus.Success, items);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "SearXNG call failed; skipping SearXNG search.");
            return new WebSearchResult(
                WebSearchStatus.ProviderError,
                [],
                "SearXNG search failed.");
        }
    }

    private static WebSearchResultItem? CreateResultItem(JsonElement result, DateTime observedAt)
    {
        var title = result.TryGetProperty("title", out var titleElement)
            ? titleElement.GetString()?.Trim()
            : null;
        var description = result.TryGetProperty("content", out var descriptionElement)
            ? descriptionElement.GetString()?.Trim()
            : null;
        var url = result.TryGetProperty("url", out var urlElement)
            ? urlElement.GetString()?.Trim()
            : null;
        if (string.IsNullOrWhiteSpace(title)
            || string.IsNullOrWhiteSpace(url)
            || !Uri.TryCreate(url, UriKind.Absolute, out var parsedUrl)
            || parsedUrl.Scheme is not ("http" or "https"))
            return null;

        return new WebSearchResultItem(title, description ?? "", url, observedAt);
    }
}
