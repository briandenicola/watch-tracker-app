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
        public const string LogLevel = "LogLevel";
        public const string OllamaUrl = "OllamaUrl";
        public const string OllamaModel = "OllamaModel";
    }

    private static readonly Dictionary<string, string> Defaults = new()
    {
        [Keys.MaxFailedAttempts] = "5",
        [Keys.LockoutDurationMinutes] = "15",
        [Keys.LogLevel] = "Information",
        [Keys.AiAnalysisPrompt] = "You are a watch expert. Analyze this watch image and provide a detailed description including the brand, model (if identifiable), movement type, case material, approximate case size, dial color, and any notable features or complications. Be concise but informative.",
        [Keys.OllamaUrl] = "http://localhost:11434",
        [Keys.OllamaModel] = ""
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
}
