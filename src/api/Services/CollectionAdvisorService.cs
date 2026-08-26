using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WatchTracker.Api.Data;
using WatchTracker.Api.DTOs;
using WatchTracker.Api.Models;

namespace WatchTracker.Api.Services;

public class CollectionAdvisorService(
    AppDbContext context,
    ICollectionProfileService collectionProfile,
    IAdvisorReplyGenerator replyGenerator,
    IRecommendationWishlistService recommendationWishlist) : ICollectionAdvisorService
{
    // The system prompt and current user turn occupy the other two request slots.
    private const int MaxModelHistoryMessages = 18;
    private const int MaxStateMessages = 100;
    private const string NotConfiguredHint =
        "The collection advisor needs Ollama. Set the Ollama URL and model under Admin -> Settings.";
    private const string WishlistNote = "Added from a Collection Advisor recommendation.";
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

    public async Task<AdvisorRecommendationFeedbackDto?> SaveFeedbackAsync(
        int messageId,
        int userId,
        SaveAdvisorFeedbackDto dto,
        CancellationToken ct = default)
    {
        var card = await FindOwnedCardAsync(messageId, userId, dto.Provider, dto.ProviderItemId, ct);
        if (card is null) return null;

        var feedback = await context.AdvisorRecommendationFeedback.FirstOrDefaultAsync(
            f => f.UserId == userId
                && f.MessageId == messageId
                && f.Provider == card.Provider
                && f.ProviderItemId == card.ProviderItemId,
            ct);
        var now = DateTime.UtcNow;
        if (feedback is null)
        {
            feedback = new AdvisorRecommendationFeedback
            {
                UserId = userId,
                MessageId = messageId,
                Provider = card.Provider!,
                ProviderItemId = card.ProviderItemId!,
                Title = card.Title,
                Kind = dto.Kind,
                CreatedAt = now,
                UpdatedAt = now
            };
            context.AdvisorRecommendationFeedback.Add(feedback);
        }

        feedback.Kind = dto.Kind;
        feedback.Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim();
        feedback.UpdatedAt = now;
        await context.SaveChangesAsync(ct);
        return ToDto(feedback);
    }

    public async Task<bool> RemoveFeedbackAsync(
        int feedbackId,
        int userId,
        CancellationToken ct = default)
    {
        var feedback = await context.AdvisorRecommendationFeedback
            .FirstOrDefaultAsync(f => f.Id == feedbackId && f.UserId == userId, ct);
        if (feedback is null) return false;

        context.AdvisorRecommendationFeedback.Remove(feedback);
        await context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<AdvisorWishlistActionResultDto?> AddToWishlistAsync(
        int messageId,
        int userId,
        AdvisorRecommendationActionDto dto,
        CancellationToken ct = default)
    {
        var card = await FindOwnedCardAsync(messageId, userId, dto.Provider, dto.ProviderItemId, ct);
        return card is null
            ? null
            : await recommendationWishlist.AddAsync(card, userId, WishlistNote, ct);
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
        var messageIds = messages.Select(m => m.Id).ToList();
        var feedback = await context.AdvisorRecommendationFeedback
            .AsNoTracking()
            .Where(f => f.UserId == session.UserId && messageIds.Contains(f.MessageId))
            .ToListAsync(ct);
        var feedbackByCard = feedback.ToDictionary(
            f => CardKey(f.MessageId, f.Provider, f.ProviderItemId),
            ToDto,
            StringComparer.OrdinalIgnoreCase);

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
                Messages = messages.Select(message => ToDto(message, feedbackByCard)).ToList()
            }
        };
    }

    private static AdvisorMessageDto ToDto(
        AdvisorMessage message,
        IReadOnlyDictionary<string, AdvisorRecommendationFeedbackDto> feedback)
    {
        var cards = Deserialize<List<AdvisorRecommendationCardDto>>(message.RecommendationCardsJson);
        foreach (var card in cards)
        {
            if (card.Provider is not null
                && card.ProviderItemId is not null
                && feedback.TryGetValue(
                    CardKey(message.Id, card.Provider, card.ProviderItemId),
                    out var cardFeedback))
                card.Feedback = cardFeedback;
        }

        return new AdvisorMessageDto
        {
            Id = message.Id,
            Role = message.Role,
            Content = message.Content,
            Citations = Deserialize<List<AdvisorCitationDto>>(message.CitationsJson),
            RecommendationCards = cards,
            FollowUps = Deserialize<List<string>>(message.FollowUpsJson),
            ToolActivity = Deserialize<List<AdvisorToolActivityDto>>(message.ToolActivityJson),
            CreatedAt = message.CreatedAt
        };
    }

    private async Task<AdvisorRecommendationCardDto?> FindOwnedCardAsync(
        int messageId,
        int userId,
        string provider,
        string providerItemId,
        CancellationToken ct)
    {
        var message = await context.AdvisorMessages
            .AsNoTracking()
            .Include(m => m.Session)
            .FirstOrDefaultAsync(
                m => m.Id == messageId
                    && m.Role == AdvisorMessageRole.Assistant
                    && m.Session.UserId == userId,
                ct);
        if (message is null) return null;

        return Deserialize<List<AdvisorRecommendationCardDto>>(message.RecommendationCardsJson)
            .FirstOrDefault(card =>
                !string.IsNullOrWhiteSpace(card.Provider)
                && !string.IsNullOrWhiteSpace(card.ProviderItemId)
                && card.Provider.Equals(provider, StringComparison.OrdinalIgnoreCase)
                && card.ProviderItemId.Equals(providerItemId, StringComparison.OrdinalIgnoreCase));
    }

    private static AdvisorRecommendationFeedbackDto ToDto(AdvisorRecommendationFeedback feedback) => new()
    {
        Id = feedback.Id,
        Kind = feedback.Kind,
        Notes = feedback.Notes,
        UpdatedAt = feedback.UpdatedAt
    };

    private static string CardKey(int messageId, string provider, string providerItemId) =>
        $"{messageId}|{provider}|{providerItemId}";

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
