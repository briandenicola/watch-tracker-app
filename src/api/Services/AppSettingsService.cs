using Microsoft.EntityFrameworkCore;
using WatchTracker.Api.Data;
using WatchTracker.Api.Models;

namespace WatchTracker.Api.Services;

public class AppSettingsService(AppDbContext context) : IAppSettingsService
{
    public static class Keys
    {
        public const string MaxFailedAttempts = "MaxFailedAttempts";
        public const string LockoutDurationMinutes = "LockoutDurationMinutes";
        public const string AiAnalysisPrompt = "AiAnalysisPrompt";
        public const string StyleAgentPrompt = "StyleAgentPrompt";
        public const string CollectionAdvisorPrompt = "CollectionAdvisorPrompt";
        public const string WatchRecommendationPrompt = "WatchRecommendationPrompt";
        public const string LogLevel = "LogLevel";
        public const string OllamaUrl = "OllamaUrl";
        public const string OllamaModel = "OllamaModel";
        public const string BraveSearchApiKey = "BraveSearchApiKey";
        public const string ResaleValueRefreshIntervalDays = "ResaleValueRefreshIntervalDays";
        public const string ResaleValuePrompt = "ResaleValuePrompt";
        public const string WebSearchProvider = "WebSearchProvider";
        public const string SearXngUrl = "SearXngUrl";
        public const string EbayClientId = "EbayClientId";
        public const string EbayClientSecret = "EbayClientSecret";
        public const string ApplicationTimeZone = "ApplicationTimeZone";
    }

    private static readonly Dictionary<string, string> Defaults = new()
    {
        [Keys.MaxFailedAttempts] = "5",
        [Keys.LockoutDurationMinutes] = "15",
        [Keys.LogLevel] = "Information",
        [Keys.AiAnalysisPrompt] = "You are a watch expert. Analyze this watch image and provide a detailed description including the brand, model (if identifiable), movement type, case material, approximate case size, dial color, and any notable features or complications. Be concise but informative.",
        [Keys.StyleAgentPrompt] = "You are a personal style consultant helping the owner of a watch collection build an outfit around one specific watch. You are warm, concrete and opinionated, you dress for the real world rather than the runway, and you work with clothes people plausibly already own.",
        [Keys.CollectionAdvisorPrompt] = "You are a practical watch collection advisor. Help the user understand collection coverage, redundancy, wear patterns, and missing metadata. Be concise, explain uncertainty, and never claim guaranteed financial returns.",
        [Keys.WatchRecommendationPrompt] = "You are an expert watch stylist. Recommend the watch that best complements the outfit, occasion, colors, formality, weather, and stated preferences. Consider visual harmony and practicality. When choices are similarly strong, favor a watch that has not been worn recently.",
        [Keys.OllamaUrl] = "http://localhost:11434",
        [Keys.OllamaModel] = "",
        [Keys.BraveSearchApiKey] = "",
        [Keys.ResaleValueRefreshIntervalDays] = "7",
        [Keys.ResaleValuePrompt] = "You are a watch resale value expert. Given web search results about a specific watch's secondhand/resale listings, estimate its current fair resale value in USD, assuming good used condition unless the listings suggest otherwise.",
        [Keys.WebSearchProvider] = "Brave",
        [Keys.SearXngUrl] = "",
        [Keys.EbayClientId] = "",
        [Keys.EbayClientSecret] = "",
        [Keys.ApplicationTimeZone] = "America/Chicago"
    };

    public async Task<string> GetAsync(string key, string defaultValue = "")
    {
        var setting = await context.AppSettings.FindAsync(key);
        if (setting is not null) return setting.Value;
        return Defaults.TryGetValue(key, out var d) ? d : defaultValue;
    }

    public async Task<int> GetIntAsync(string key, int defaultValue)
    {
        var val = await GetAsync(key);
        return int.TryParse(val, out var result) ? result : defaultValue;
    }

    public async Task SetAsync(string key, string value)
    {
        var setting = await context.AppSettings.FindAsync(key);
        if (setting is null)
        {
            context.AppSettings.Add(new AppSetting { Key = key, Value = value });
        }
        else
        {
            setting.Value = value;
        }
        await context.SaveChangesAsync();
    }

    public async Task<Dictionary<string, string>> GetAllAsync()
    {
        var stored = await context.AppSettings.ToDictionaryAsync(s => s.Key, s => s.Value);
        foreach (var (key, val) in Defaults)
        {
            stored.TryAdd(key, val);
        }
        return stored;
    }

    public static bool IsValidTimeZoneId(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        if (value != "UTC"
            && !TimeZoneInfo.TryConvertIanaIdToWindowsId(value, out _))
        {
            return false;
        }

        try
        {
            _ = TimeZoneInfo.FindSystemTimeZoneById(value);
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            return false;
        }
        catch (InvalidTimeZoneException)
        {
            return false;
        }
    }
}
