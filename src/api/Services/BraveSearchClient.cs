using System.Text.Json;

namespace WatchTracker.Api.Services;

public class BraveSearchClient(
    HttpClient httpClient,
    IAppSettingsService appSettings,
    ILogger<BraveSearchClient> logger) : IWebSearchClient
{
    public string ProviderName => "Brave";

    public async Task<List<WebSearchResultItem>> SearchAsync(string query, CancellationToken ct = default)
    {
        var apiKey = await appSettings.GetAsync(AppSettingsService.Keys.BraveSearchApiKey);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            logger.LogInformation("Brave Search API key is not configured; skipping web search leg.");
            return [];
        }

        var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"https://api.search.brave.com/res/v1/web/search?q={Uri.EscapeDataString(query)}&count=10");
        request.Headers.Add("Accept", "application/json");
        request.Headers.Add("X-Subscription-Token", apiKey);

        try
        {
            var response = await httpClient.SendAsync(request, ct);
            var body = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Brave Search API error {Status}: {Body}", response.StatusCode, body);
                return [];
            }

            using var doc = JsonDocument.Parse(body);
            if (!doc.RootElement.TryGetProperty("web", out var web) ||
                !web.TryGetProperty("results", out var results))
                return [];

            return results.EnumerateArray()
                .Select(r => new WebSearchResultItem(
                    r.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "",
                    r.TryGetProperty("description", out var d) ? d.GetString() ?? "" : "",
                    r.TryGetProperty("url", out var u) ? u.GetString() ?? "" : ""))
                .ToList();
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Brave Search call failed; skipping web search leg.");
            return [];
        }
    }
}
