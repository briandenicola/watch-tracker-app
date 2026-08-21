using WatchTracker.Api.DTOs;
using WatchTracker.Api.Models;

namespace WatchTracker.Api.Services;

public record AdvisorGeneratedReply(
    string Content,
    IReadOnlyList<AdvisorCitationDto> Citations,
    IReadOnlyList<AdvisorRecommendationCardDto> RecommendationCards,
    IReadOnlyList<string> FollowUps,
    IReadOnlyList<AdvisorToolActivityDto> ToolActivity);

public interface IAdvisorReplyGenerator
{
    Task<bool> IsConfiguredAsync();

    Task<AdvisorGeneratedReply> GenerateAsync(
        int userId,
        CollectionProfileDto profile,
        IReadOnlyList<AdvisorMessage> history,
        string userMessage,
        CancellationToken ct = default);
}
