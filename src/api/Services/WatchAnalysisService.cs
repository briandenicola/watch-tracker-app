using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WatchTracker.Api.Data;

namespace WatchTracker.Api.Services;

public class WatchAnalysisService(
    AppDbContext context,
    IAppSettingsService appSettings,
    IConfiguration configuration,
    HttpClient httpClient,
    IWebHostEnvironment env) : IWatchAnalysisService
{
    public async Task<string> AnalyzeAsync(int watchId, int userId)
    {
        var watch = await context.Watches
            .Include(w => w.Images)
            .FirstOrDefaultAsync(w => w.Id == watchId && w.UserId == userId)
            ?? throw new InvalidOperationException("Watch not found.");

        var image = watch.Images.OrderBy(i => i.SortOrder).FirstOrDefault()
            ?? throw new InvalidOperationException("No images to analyze.");

        var filePath = Path.Combine(env.ContentRootPath, "uploads", image.FileName);
        if (!File.Exists(filePath))
            throw new InvalidOperationException("Image file not found.");

        var imageBytes = await File.ReadAllBytesAsync(filePath);
        var base64 = Convert.ToBase64String(imageBytes);

        var prompt = await appSettings.GetAsync(
            AppSettingsService.Keys.AiAnalysisPrompt,
            "Analyze this watch image and describe the watch.");

        var provider = await appSettings.GetAsync(AppSettingsService.Keys.AiProvider, "Anthropic");

        return provider.Equals("Ollama", StringComparison.OrdinalIgnoreCase)
            ? await AnalyzeWithOllamaAsync(base64, prompt)
            : await AnalyzeWithAnthropicAsync(base64, image.ContentType, prompt);
    }

    private async Task<string> AnalyzeWithAnthropicAsync(string base64, string contentType, string prompt)
    {
        var apiKey = configuration["Anthropic:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
            apiKey = await appSettings.GetAsync("AnthropicApiKey");
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("Anthropic API key is not configured.");

        var requestBody = new
        {
            model = "claude-sonnet-4-20250514",
            max_tokens = 1024,
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new
                        {
                            type = "image",
                            source = new
                            {
                                type = "base64",
                                media_type = contentType,
                                data = base64
                            }
                        },
                        new
                        {
                            type = "text",
                            text = prompt
                        }
                    }
                }
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("x-api-key", apiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");

        var response = await httpClient.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Anthropic API error: {responseBody}");

        using var doc = JsonDocument.Parse(responseBody);
        var content = doc.RootElement.GetProperty("content");
        var textBlock = content.EnumerateArray()
            .FirstOrDefault(e => e.GetProperty("type").GetString() == "text");
        var analysis = textBlock.GetProperty("text").GetString()
            ?? throw new InvalidOperationException("No text in Anthropic response.");

        return analysis;
    }

    private async Task<string> AnalyzeWithOllamaAsync(string base64, string prompt)
    {
        var ollamaUrl = await appSettings.GetAsync(AppSettingsService.Keys.OllamaUrl, "http://localhost:11434");
        if (string.IsNullOrWhiteSpace(ollamaUrl))
            throw new InvalidOperationException("Ollama URL is not configured.");

        var model = await appSettings.GetAsync(AppSettingsService.Keys.OllamaModel);
        if (string.IsNullOrWhiteSpace(model))
            throw new InvalidOperationException("Ollama model is not configured.");

        var requestBody = new
        {
            model,
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = prompt,
                    images = new[] { base64 }
                }
            },
            stream = false
        };

        var json = JsonSerializer.Serialize(requestBody);
        var request = new HttpRequestMessage(HttpMethod.Post, $"{ollamaUrl.TrimEnd('/')}/api/chat")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        var response = await httpClient.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Ollama API error: {responseBody}");

        using var doc = JsonDocument.Parse(responseBody);
        var message = doc.RootElement.GetProperty("message");
        var analysis = message.GetProperty("content").GetString()
            ?? throw new InvalidOperationException("No content in Ollama response.");

        return analysis;
    }

    public async Task<List<string>> GetOllamaModelsAsync(string url)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{url.TrimEnd('/')}/api/tags");
        var response = await httpClient.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Failed to connect to Ollama at {url}");

        using var doc = JsonDocument.Parse(responseBody);
        var models = doc.RootElement.GetProperty("models");
        return models.EnumerateArray()
            .Select(m => m.GetProperty("name").GetString()!)
            .OrderBy(n => n)
            .ToList();
    }
}
