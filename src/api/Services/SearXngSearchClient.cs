using System.Text.Json;

namespace WatchTracker.Api.Services;

public class SearXngSearchClient(
    HttpClient httpClient,
    IAppSettingsService appSettings,
    ILogger<SearXngSearchClient> logger) : IWebSearchClient
{
    public string ProviderName => "SearXNG";

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
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "SearXNG call failed; skipping SearXNG search.");
            return [];
        }
    }
}
