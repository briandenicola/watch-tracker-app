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

    public async Task<List<WebSearchResultItem>> SearchAsync(string query, CancellationToken ct = default)
    {
        var baseUrl = await appSettings.GetAsync(AppSettingsService.Keys.SearXngUrl);
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            logger.LogInformation("SearXNG URL is not configured; skipping SearXNG search.");
            return [];
        }

        var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{baseUrl.TrimEnd('/')}/search?q={Uri.EscapeDataString(query)}&format=json");
        request.Headers.Add("Accept", "application/json");

        try
        {
            var response = await httpClient.SendAsync(request, ct);
            var body = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("SearXNG API error {Status}: {Body}", response.StatusCode, body);
                return [];
            }

            using var doc = JsonDocument.Parse(body);
            if (!doc.RootElement.TryGetProperty("results", out var results))
                return [];

            return results.EnumerateArray()
                .Take(10)
                .Select(r => new WebSearchResultItem(
                    r.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "",
                    r.TryGetProperty("content", out var c) ? c.GetString() ?? "" : "",
                    r.TryGetProperty("url", out var u) ? u.GetString() ?? "" : ""))
                .ToList();
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "SearXNG call failed; skipping SearXNG search.");
            return [];
        }
    }
}
