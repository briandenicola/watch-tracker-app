namespace WatchTracker.Api.Services;

public interface IWatchAnalysisService
{
    Task<string> AnalyzeAsync(int watchId, int userId);
}
