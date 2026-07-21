using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace WatchTracker.Api.Services;

public class EbayTokenProvider(
    IHttpClientFactory httpClientFactory,
    IServiceScopeFactory scopeFactory,
    ILogger<EbayTokenProvider> logger) : IEbayTokenProvider
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private string? _cachedToken;
    private DateTimeOffset _expiresAt = DateTimeOffset.MinValue;

    public async Task<string?> GetAccessTokenAsync(CancellationToken ct = default)
    {
        if (_cachedToken is not null && DateTimeOffset.UtcNow < _expiresAt)
            return _cachedToken;

        await _lock.WaitAsync(ct);
        try
        {
            if (_cachedToken is not null && DateTimeOffset.UtcNow < _expiresAt)
                return _cachedToken;

            string clientId, clientSecret;
            using (var scope = scopeFactory.CreateScope())
            {
                var appSettings = scope.ServiceProvider.GetRequiredService<IAppSettingsService>();
                clientId = await appSettings.GetAsync(AppSettingsService.Keys.EbayClientId);
                clientSecret = await appSettings.GetAsync(AppSettingsService.Keys.EbayClientSecret);
            }
            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
            {
                logger.LogInformation("eBay API credentials are not configured; skipping eBay leg.");
                return null;
            }

            var httpClient = httpClientFactory.CreateClient("EbayToken");
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.ebay.com/identity/v1/oauth2/token");
            var basicAuth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basicAuth);
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["scope"] = "https://api.ebay.com/oauth/api_scope"
            });

            var response = await httpClient.SendAsync(request, ct);
            var body = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("eBay OAuth token request failed {Status}: {Body}", response.StatusCode, body);
                return null;
            }

            using var doc = JsonDocument.Parse(body);
            var token = doc.RootElement.GetProperty("access_token").GetString();
            var expiresIn = doc.RootElement.TryGetProperty("expires_in", out var e) ? e.GetInt32() : 7200;
            if (string.IsNullOrEmpty(token)) return null;

            _cachedToken = token;
            _expiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresIn - 60);
            return _cachedToken;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "eBay OAuth token request threw; skipping eBay leg.");
            return null;
        }
        finally
        {
            _lock.Release();
        }
    }
}
