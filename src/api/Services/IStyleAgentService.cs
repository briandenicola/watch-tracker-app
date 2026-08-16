using WatchTracker.Api.DTOs;

namespace WatchTracker.Api.Services;

public interface IStyleAgentService
{
    /// <summary>The current conversation and remembered recommendations for a watch.</summary>
    Task<StyleChatStateDto?> GetStateAsync(int watchId, int userId, CancellationToken ct = default);

    /// <summary>Sends a turn to the agent and returns the updated conversation.</summary>
    Task<StyleChatStateDto?> SendMessageAsync(int watchId, int userId, SendStyleMessageDto dto, CancellationToken ct = default);

    /// <summary>Starts a fresh conversation. Remembered recommendations are kept.</summary>
    Task<StyleChatStateDto?> StartNewSessionAsync(int watchId, int userId, CancellationToken ct = default);

    /// <summary>Records whether a remembered recommendation worked out.</summary>
    Task<StyleRecommendationDto?> RecordFeedbackAsync(
        int watchId, int recommendationId, int userId, StyleFeedbackDto dto, CancellationToken ct = default);

    /// <summary>Drops a recommendation from the agent's memory.</summary>
    Task<bool> ForgetRecommendationAsync(int watchId, int recommendationId, int userId, CancellationToken ct = default);
}
