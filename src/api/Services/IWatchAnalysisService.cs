using WatchTracker.Api.DTOs;

namespace WatchTracker.Api.Services;

public interface IWatchAnalysisService
{
    /// <summary>
    /// Describes the watch in a few lines and proposes values for the fields it
    /// has none. The description is saved; the suggestions are not — they come
    /// back for the owner to approve.
    /// </summary>
    Task<WatchAnalysisResultDto> AnalyzeAsync(int watchId, int userId, CancellationToken ct = default);

    /// <summary>Writes the values the owner approved, skipping anything that fails validation.</summary>
    Task<ApplyAnalysisResultDto?> ApplySuggestionsAsync(
        int watchId, int userId, ApplyAnalysisSuggestionsDto dto, CancellationToken ct = default);

    Task<List<string>> GetOllamaModelsAsync(string url, CancellationToken ct = default);
}
