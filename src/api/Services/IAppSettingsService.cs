namespace WatchTracker.Api.Services;

public interface IAppSettingsService
{
    Task<string> GetAsync(string key, string defaultValue = "");
    Task<int> GetIntAsync(string key, int defaultValue);
    Task SetAsync(string key, string value);
    Task<Dictionary<string, string>> GetAllAsync();
}
