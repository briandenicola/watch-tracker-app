using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WatchTracker.Api.Data;
using WatchTracker.Api.DTOs;
using WatchTracker.Api.Models;

namespace WatchTracker.Api.Services;

public class CollectionAdvisorService(
    AppDbContext context,
    ICollectionProfileService collectionProfile,
    IAdvisorReplyGenerator replyGenerator) : ICollectionAdvisorService
{
    // The system prompt and current user turn occupy the other two request slots.
    private const int MaxModelHistoryMessages = 18;
    private const int MaxStateMessages = 100;
    private const string NotConfiguredHint =
        "The collection advisor needs Ollama. Set the Ollama URL and model under Admin -> Settings.";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<AdvisorChatStateDto> GetCurrentStateAsync(
        int userId,
        CancellationToken ct = default)
    {
        var session = await context.AdvisorSessions
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.UpdatedAt)
            .ThenByDescending(s => s.Id)
            .FirstOrDefaultAsync(ct);

        if (session is null)
        {
            session = new AdvisorSession { UserId = userId };
            context.AdvisorSessions.Add(session);
            await context.SaveChangesAsync(ct);
        }

        return await BuildStateAsync(session, ct);
    }

    public async Task<AdvisorChatStateDto?> GetStateAsync(
        int sessionId,
        int userId,
        CancellationToken ct = default)
    {
        var session = await FindOwnedSessionAsync(sessionId, userId, ct);
        return session is null ? null : await BuildStateAsync(session, ct);
    }

    public async Task<AdvisorChatStateDto> StartNewSessionAsync(
        int userId,
        CancellationToken ct = default)
    {
        var session = new AdvisorSession { UserId = userId };
        context.AdvisorSessions.Add(session);
        await context.SaveChangesAsync(ct);
        return await BuildStateAsync(session, ct);
    }

    public async Task<AdvisorChatStateDto?> SendMessageAsync(
        int sessionId,
        int userId,
        SendAdvisorMessageDto dto,
        CancellationToken ct = default)
    {
        var session = await FindOwnedSessionAsync(sessionId, userId, ct);
        if (session is null) return null;

        var userMessage = dto.Message.Trim();
        if (userMessage.Length == 0)
            throw new InvalidOperationException("Type a message before sending.");

        var history = await context.AdvisorMessages
            .AsNoTracking()
            .Where(m => m.SessionId == session.Id)
            .OrderByDescending(m => m.Id)
            .Take(MaxModelHistoryMessages)
            .ToListAsync(ct);
        history.Reverse();

        var profile = await collectionProfile.GetProfileAsync(userId, ct);
        var reply = await replyGenerator.GenerateAsync(userId, profile, history, userMessage, ct);
        if (string.IsNullOrWhiteSpace(reply.Content))
            throw new InvalidOperationException("The collection advisor returned an empty response.");

        var now = DateTime.UtcNow;
        await using var transaction = await context.Database.BeginTransactionAsync(ct);
        context.AdvisorMessages.AddRange(
            new AdvisorMessage
            {
                SessionId = session.Id,
                Role = AdvisorMessageRole.User,
                Content = userMessage,
                CreatedAt = now
            },
            new AdvisorMessage
            {
                SessionId = session.Id,
                Role = AdvisorMessageRole.Assistant,
                Content = reply.Content.Trim(),
                CitationsJson = JsonSerializer.Serialize(reply.Citations, JsonOptions),
                RecommendationCardsJson = JsonSerializer.Serialize(reply.RecommendationCards, JsonOptions),
                FollowUpsJson = JsonSerializer.Serialize(reply.FollowUps, JsonOptions),
                ToolActivityJson = JsonSerializer.Serialize(reply.ToolActivity, JsonOptions),
                CreatedAt = now.AddTicks(1)
            });
        session.UpdatedAt = now;
        await context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return await BuildStateAsync(session, ct);
    }

    private Task<AdvisorSession?> FindOwnedSessionAsync(
        int sessionId,
        int userId,
        CancellationToken ct) =>
        context.AdvisorSessions.FirstOrDefaultAsync(
            s => s.Id == sessionId && s.UserId == userId,
            ct);

    private async Task<AdvisorChatStateDto> BuildStateAsync(
        AdvisorSession session,
        CancellationToken ct)
    {
        var messages = await context.AdvisorMessages
            .AsNoTracking()
            .Where(m => m.SessionId == session.Id)
            .OrderByDescending(m => m.Id)
            .Take(MaxStateMessages)
            .ToListAsync(ct);
        messages.Reverse();

        var configured = await replyGenerator.IsConfiguredAsync();
        return new AdvisorChatStateDto
        {
            Configured = configured,
            ConfigurationHint = configured ? null : NotConfiguredHint,
            Session = new AdvisorSessionDto
            {
                Id = session.Id,
                CreatedAt = session.CreatedAt,
                UpdatedAt = session.UpdatedAt,
                Messages = messages.Select(ToDto).ToList()
            }
        };
    }

    private static AdvisorMessageDto ToDto(AdvisorMessage message) => new()
    {
        Id = message.Id,
        Role = message.Role,
        Content = message.Content,
        Citations = Deserialize<List<AdvisorCitationDto>>(message.CitationsJson),
        RecommendationCards = Deserialize<List<AdvisorRecommendationCardDto>>(message.RecommendationCardsJson),
        FollowUps = Deserialize<List<string>>(message.FollowUpsJson),
        ToolActivity = Deserialize<List<AdvisorToolActivityDto>>(message.ToolActivityJson),
        CreatedAt = message.CreatedAt
    };

    private static T Deserialize<T>(string json) where T : new()
    {
        try
        {
            return JsonSerializer.Deserialize<T>(json, JsonOptions) ?? new T();
        }
        catch (JsonException)
        {
            throw new InvalidOperationException("Stored collection advisor message data is invalid.");
        }
    }
}
