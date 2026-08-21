using System.Text.Json;

namespace WatchTracker.Api.Services;

public class BraveSearchClient(
    HttpClient httpClient,
    IAppSettingsService appSettings,
    ILogger<BraveSearchClient> logger) : IWebSearchClient
{
    public string ProviderName => "Brave";

    public async Task<WebSearchResult> SearchAsync(string query, CancellationToken ct = default)
    {
        var apiKey = await appSettings.GetAsync(AppSettingsService.Keys.BraveSearchApiKey);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            logger.LogInformation("Brave Search API key is not configured; skipping web search leg.");
            return new WebSearchResult(
                WebSearchStatus.NotConfigured,
                [],
                "Brave Search is not configured.");
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"https://api.search.brave.com/res/v1/web/search?q={Uri.EscapeDataString(query)}&count=10");
        request.Headers.Add("Accept", "application/json");
        request.Headers.Add("X-Subscription-Token", apiKey);

        try
        {
            using var response = await httpClient.SendAsync(request, ct);
            var body = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Brave Search request failed with HTTP {StatusCode}.",
                    (int)response.StatusCode);
                return new WebSearchResult(
                    WebSearchStatus.ProviderError,
                    [],
                    $"Brave Search returned HTTP {(int)response.StatusCode}.");
            }

            using var doc = JsonDocument.Parse(body);
            if (!doc.RootElement.TryGetProperty("web", out var web) ||
                !web.TryGetProperty("results", out var results))
                return new WebSearchResult(WebSearchStatus.Success, []);

            var observedAt = DateTime.UtcNow;
            var items = results.EnumerateArray()
                .Select(r => CreateResultItem(r, observedAt))
                .Where(r => r is not null)
                .Select(r => r!)
                .Take(10)
                .ToList();
            return new WebSearchResult(WebSearchStatus.Success, items);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            logger.LogWarning("Brave Search call failed; skipping web search leg.");
            return new WebSearchResult(
                WebSearchStatus.ProviderError,
                [],
                "Brave Search failed.");
        }
    }

    private static WebSearchResultItem? CreateResultItem(JsonElement result, DateTime observedAt)
    {
        var title = result.TryGetProperty("title", out var titleElement)
            ? titleElement.GetString()?.Trim()
            : null;
        var description = result.TryGetProperty("description", out var descriptionElement)
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
