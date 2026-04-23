namespace WatchTracker.Api.Services;

public interface IWatchAnalysisService
{
    Task<string> AnalyzeAsync(int watchId, int userId, CancellationToken ct = default);
    Task<List<string>> GetOllamaModelsAsync(string url, CancellationToken ct = default);
}
