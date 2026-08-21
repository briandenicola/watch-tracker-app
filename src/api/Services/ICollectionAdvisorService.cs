using WatchTracker.Api.DTOs;

namespace WatchTracker.Api.Services;

public interface ICollectionAdvisorService
{
    Task<AdvisorChatStateDto> GetCurrentStateAsync(int userId, CancellationToken ct = default);
    Task<AdvisorChatStateDto?> GetStateAsync(int sessionId, int userId, CancellationToken ct = default);
    Task<AdvisorChatStateDto> StartNewSessionAsync(int userId, CancellationToken ct = default);
    Task<AdvisorChatStateDto?> SendMessageAsync(
        int sessionId,
        int userId,
        SendAdvisorMessageDto dto,
        CancellationToken ct = default);
}
