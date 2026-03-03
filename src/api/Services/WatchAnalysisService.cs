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

        var apiKey = configuration["Anthropic:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
            apiKey = await appSettings.GetAsync("AnthropicApiKey");
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("Anthropic API key is not configured.");

        var prompt = await appSettings.GetAsync(
            AppSettingsService.Keys.AiAnalysisPrompt,
            "Analyze this watch image and describe the watch.");

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
                                media_type = image.ContentType,
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

        watch.AiAnalysis = analysis;
        watch.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        return analysis;
    }
}
