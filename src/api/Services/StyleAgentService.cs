using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using WatchTracker.Api.Data;
using WatchTracker.Api.DTOs;
using WatchTracker.Api.Models;

namespace WatchTracker.Api.Services;

/// <summary>
/// A chat agent that styles an outfit around one watch. Each reply is grounded
/// in the watch's own details plus the recommendations it has made before —
/// including whatever the user said about how those turned out — so the advice
/// gets more personal the longer the collection is used.
/// </summary>
public class StyleAgentService(
    AppDbContext context,
    IAppSettingsService appSettings,
    HttpClient httpClient,
    IWebHostEnvironment env,
    ILogger<StyleAgentService> logger) : IStyleAgentService
{
    /// Turns of the running conversation replayed to the model.
    private const int MaxHistoryMessages = 20;
    /// Remembered recommendations for this watch put in front of the model.
    private const int MaxWatchMemoriesInPrompt = 8;
    /// Recent recommendations for the rest of the collection, for general taste.
    private const int MaxCollectionMemoriesInPrompt = 5;
    /// Remembered recommendations returned to the chat's memory panel.
    private const int MaxMemoriesReturned = 25;
    private const int MaxFollowUps = 3;
    /// Longest edge of the watch photo sent to the model, in pixels.
    private const int MaxPhotoEdge = 768;
    private const int PhotoQuality = 80;

    private const string NotConfiguredHint =
        "The style agent needs Ollama. Set the Ollama URL and model under Admin → Settings.";

    public async Task<StyleChatStateDto?> GetStateAsync(int watchId, int userId, CancellationToken ct = default)
    {
        var watch = await FindWatchAsync(watchId, userId, ct);
        if (watch is null) return null;

        var session = await GetOrCreateSessionAsync(watchId, userId, ct);
        return await BuildStateAsync(watchId, userId, session, [], ct);
    }

    public async Task<StyleChatStateDto?> StartNewSessionAsync(int watchId, int userId, CancellationToken ct = default)
    {
        var watch = await FindWatchAsync(watchId, userId, ct);
        if (watch is null) return null;

        var session = await GetOrCreateSessionAsync(watchId, userId, ct);

        // An untouched conversation is already a fresh one, so don't leave a
        // trail of empty sessions behind repeated taps of "New chat".
        if (await context.StyleMessages.AnyAsync(m => m.SessionId == session.Id, ct))
        {
            session = new StyleSession { WatchId = watchId, UserId = userId };
            context.StyleSessions.Add(session);
            await context.SaveChangesAsync(ct);
        }

        return await BuildStateAsync(watchId, userId, session, [], ct);
    }

    public async Task<StyleChatStateDto?> SendMessageAsync(
        int watchId, int userId, SendStyleMessageDto dto, CancellationToken ct = default)
    {
        var watch = await FindWatchAsync(watchId, userId, ct);
        if (watch is null) return null;

        var config = await TryGetOllamaConfigAsync()
            ?? throw new InvalidOperationException(NotConfiguredHint);

        var occasion = Normalize(dto.Occasion);
        var weather = Normalize(dto.Weather);
        var turn = BuildUserTurn(dto.Message, occasion, weather);
        if (turn is null)
            throw new InvalidOperationException("Type a message, or set the occasion or weather, before sending.");

        var session = await GetOrCreateSessionAsync(watchId, userId, ct);
        if (occasion is not null) session.Occasion = occasion;
        if (weather is not null) session.Weather = weather;

        var history = await context.StyleMessages
            .Where(m => m.SessionId == session.Id)
            .OrderByDescending(m => m.Id)
            .Take(MaxHistoryMessages)
            .ToListAsync(ct);
        history.Reverse();

        var watchMemory = await LoadWatchMemoryAsync(watchId, userId, MaxWatchMemoriesInPrompt, ct);
        var collectionMemory = await LoadCollectionMemoryAsync(watchId, userId, ct);
        var photo = await TryLoadCoverPhotoAsync(watchId, ct);
        var systemPrompt = await BuildSystemPromptAsync(watch, session, watchMemory, collectionMemory);

        // Nothing is written until the model has answered, so a failed call
        // leaves the transcript unchanged and the user can simply send again.
        var reply = await AskOllamaAsync(config.Url, config.Model, systemPrompt, history, turn, photo, ct);

        var now = DateTime.UtcNow;
        StyleRecommendation? recommendation = null;
        if (reply.Recommendation is { } parsed)
        {
            recommendation = new StyleRecommendation
            {
                WatchId = watchId,
                UserId = userId,
                SessionId = session.Id,
                Occasion = parsed.Occasion ?? session.Occasion,
                Weather = parsed.Weather ?? session.Weather,
                Summary = parsed.Summary,
                Outfit = parsed.Outfit,
                CreatedAt = now
            };
            context.StyleRecommendations.Add(recommendation);
        }

        context.StyleMessages.Add(new StyleMessage
        {
            SessionId = session.Id,
            Role = StyleMessageRole.User,
            Content = turn,
            CreatedAt = now
        });

        var assistantMessage = new StyleMessage
        {
            SessionId = session.Id,
            Role = StyleMessageRole.Assistant,
            Content = reply.Text,
            Recommendation = recommendation,
            CreatedAt = now.AddTicks(1)
        };
        context.StyleMessages.Add(assistantMessage);

        session.UpdatedAt = now;
        await context.SaveChangesAsync(ct);

        return await BuildStateAsync(watchId, userId, session, reply.FollowUps, ct);
    }

    public async Task<StyleRecommendationDto?> RecordFeedbackAsync(
        int watchId, int recommendationId, int userId, StyleFeedbackDto dto, CancellationToken ct = default)
    {
        var recommendation = await context.StyleRecommendations
            .FirstOrDefaultAsync(r => r.Id == recommendationId && r.WatchId == watchId && r.UserId == userId, ct);
        if (recommendation is null) return null;

        recommendation.WasHelpful = dto.WasHelpful;
        recommendation.FeedbackNotes = Normalize(dto.Notes);
        recommendation.FeedbackAt = DateTime.UtcNow;
        await context.SaveChangesAsync(ct);

        return ToRecommendationDto(recommendation);
    }

    public async Task<bool> ForgetRecommendationAsync(
        int watchId, int recommendationId, int userId, CancellationToken ct = default)
    {
        var recommendation = await context.StyleRecommendations
            .FirstOrDefaultAsync(r => r.Id == recommendationId && r.WatchId == watchId && r.UserId == userId, ct);
        if (recommendation is null) return false;

        context.StyleRecommendations.Remove(recommendation);
        await context.SaveChangesAsync(ct);
        return true;
    }

    // --- State -------------------------------------------------------------

    private Task<Watch?> FindWatchAsync(int watchId, int userId, CancellationToken ct) =>
        context.Watches.FirstOrDefaultAsync(w => w.Id == watchId && w.UserId == userId, ct);

    private async Task<StyleSession> GetOrCreateSessionAsync(int watchId, int userId, CancellationToken ct)
    {
        var session = await context.StyleSessions
            .Where(s => s.WatchId == watchId && s.UserId == userId)
            .OrderByDescending(s => s.Id)
            .FirstOrDefaultAsync(ct);

        if (session is not null) return session;

        session = new StyleSession { WatchId = watchId, UserId = userId };
        context.StyleSessions.Add(session);
        await context.SaveChangesAsync(ct);
        return session;
    }

    private async Task<StyleChatStateDto> BuildStateAsync(
        int watchId, int userId, StyleSession session, List<string> followUps, CancellationToken ct)
    {
        var messages = await context.StyleMessages
            .Where(m => m.SessionId == session.Id)
            .Include(m => m.Recommendation)
            .OrderBy(m => m.Id)
            .ToListAsync(ct);

        var memory = await LoadWatchMemoryAsync(watchId, userId, MaxMemoriesReturned, ct);
        var configured = await TryGetOllamaConfigAsync() is not null;

        return new StyleChatStateDto
        {
            Configured = configured,
            ConfigurationHint = configured ? null : NotConfiguredHint,
            Session = new StyleSessionDto
            {
                Id = session.Id,
                Occasion = session.Occasion,
                Weather = session.Weather,
                CreatedAt = session.CreatedAt,
                UpdatedAt = session.UpdatedAt,
                Messages = messages.Select(ToMessageDto).ToList()
            },
            Memory = memory.Select(ToRecommendationDto).ToList(),
            FollowUps = followUps
        };
    }

    private Task<List<StyleRecommendation>> LoadWatchMemoryAsync(
        int watchId, int userId, int limit, CancellationToken ct) =>
        context.StyleRecommendations
            .Where(r => r.WatchId == watchId && r.UserId == userId)
            .OrderByDescending(r => r.Id)
            .Take(limit)
            .ToListAsync(ct);

    /// <summary>
    /// Rated recommendations for the user's other watches. Only rated ones carry
    /// a taste signal worth spending prompt space on.
    /// </summary>
    private Task<List<StyleRecommendation>> LoadCollectionMemoryAsync(int watchId, int userId, CancellationToken ct) =>
        context.StyleRecommendations
            .Where(r => r.UserId == userId && r.WatchId != watchId && r.WasHelpful != null)
            .Include(r => r.Watch)
            .OrderByDescending(r => r.Id)
            .Take(MaxCollectionMemoriesInPrompt)
            .ToListAsync(ct);

    private static StyleMessageDto ToMessageDto(StyleMessage message) => new()
    {
        Id = message.Id,
        Role = message.Role,
        Content = message.Content,
        Recommendation = message.Recommendation is null ? null : ToRecommendationDto(message.Recommendation),
        CreatedAt = message.CreatedAt
    };

    private static StyleRecommendationDto ToRecommendationDto(StyleRecommendation recommendation) => new()
    {
        Id = recommendation.Id,
        WatchId = recommendation.WatchId,
        Occasion = recommendation.Occasion,
        Weather = recommendation.Weather,
        Summary = recommendation.Summary,
        Outfit = recommendation.Outfit,
        WasHelpful = recommendation.WasHelpful,
        FeedbackNotes = recommendation.FeedbackNotes,
        FeedbackAt = recommendation.FeedbackAt,
        CreatedAt = recommendation.CreatedAt
    };

    // --- The watch's photo -------------------------------------------------

    /// <summary>
    /// The watch's cover image, downscaled and re-encoded as base64 JPEG for the
    /// model to look at. Colour and finish are what a stylist needs most and are
    /// exactly what the text fields tend to be missing, so the picture does more
    /// work here than any field. Null when there is no usable image — the chat
    /// still works, it just has less to go on.
    /// </summary>
    private async Task<string?> TryLoadCoverPhotoAsync(int watchId, CancellationToken ct)
    {
        var image = await context.WatchImages
            .Where(i => i.WatchId == watchId)
            .OrderBy(i => i.SortOrder)
            .FirstOrDefaultAsync(ct);
        if (image is null) return null;

        var path = Path.Combine(env.ContentRootPath, "uploads", image.FileName);
        if (!File.Exists(path)) return null;

        try
        {
            using var photo = await Image.LoadAsync<Rgba32>(path, ct);
            var oversized = photo.Width > MaxPhotoEdge || photo.Height > MaxPhotoEdge;

            photo.Mutate(ctx =>
            {
                // Background removal leaves transparent PNGs, and JPEG has no
                // alpha — without this the cut-out watch lands on black.
                ctx.BackgroundColor(Color.White);

                // Only ever down. Blowing a small image up would cost payload
                // and add nothing for the model to see.
                if (oversized)
                {
                    ctx.Resize(new ResizeOptions
                    {
                        Size = new Size(MaxPhotoEdge, MaxPhotoEdge),
                        Mode = ResizeMode.Max
                    });
                }
            });

            using var buffer = new MemoryStream();
            await photo.SaveAsJpegAsync(buffer, new JpegEncoder { Quality = PhotoQuality }, ct);

            return Convert.ToBase64String(buffer.ToArray());
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not prepare the photo of watch {WatchId} for the style agent.", watchId);
            return null;
        }
    }

    // --- Prompt ------------------------------------------------------------

    private async Task<string> BuildSystemPromptAsync(
        Watch watch,
        StyleSession session,
        IReadOnlyList<StyleRecommendation> watchMemory,
        IReadOnlyList<StyleRecommendation> collectionMemory)
    {
        var persona = await appSettings.GetAsync(AppSettingsService.Keys.StyleAgentPrompt);

        var prompt = new StringBuilder();
        prompt.AppendLine(persona.Trim());
        prompt.AppendLine();

        prompt.AppendLine("## The watch you are styling");
        prompt.AppendLine(DescribeWatch(watch));
        prompt.AppendLine();

        prompt.AppendLine("## What the user has told you so far");
        prompt.AppendLine($"- Occasion: {session.Occasion ?? "not given yet — ask"}");
        prompt.AppendLine($"- Weather: {session.Weather ?? "not given yet — ask"}");
        prompt.AppendLine();

        prompt.AppendLine("## Outfits you have already recommended for this watch");
        if (watchMemory.Count == 0)
        {
            prompt.AppendLine("- None yet. This is your first recommendation for it.");
        }
        else
        {
            foreach (var memory in watchMemory)
                prompt.AppendLine(DescribeMemory(memory, null));
        }
        prompt.AppendLine();

        if (collectionMemory.Count > 0)
        {
            prompt.AppendLine("## Rated outfits for the user's other watches, as a guide to their taste");
            foreach (var memory in collectionMemory)
                prompt.AppendLine(DescribeMemory(memory, $"{memory.Watch.Brand} {memory.Watch.Model}"));
            prompt.AppendLine();
        }

        prompt.Append(AnswerRules);
        return prompt.ToString();
    }

    private const string AnswerRules = """
        ## How to answer
        - Recommend an outfit only once you know both the occasion and the weather. If either is missing, ask for it in one short, friendly question instead of guessing.
        - Never repeat an outfit the user called unhelpful. Lean into whatever they called helpful.
        - If a remembered recommendation has no feedback yet, ask — in one sentence at the end of your reply — whether it worked out.
        - Keep replies under 180 words and use markdown. Name concrete garments, colours, materials and footwear, and say how each plays off the watch's case, dial or strap.
        - Reply with a single JSON object and nothing else, in exactly this shape:
          {"reply": "<your markdown answer>", "followUps": ["<a short question or quick reply the user can tap>"], "recommendation": {"summary": "<one line naming the outfit>", "occasion": "<occasion>", "weather": "<weather>", "outfit": "<the full outfit as markdown>"}}
        - Set "recommendation" to null on any turn where you are only asking questions, and keep "followUps" to at most three short items.
        """;

    private const string PhotoNote = """
        ## The photo
        A photo of this watch is attached to the user's latest message. Look at it, and trust what you see — dial, bezel, strap, metal colour and finish — over the recorded fields above, which are often blank or out of date. Name the colours you are working with in your reply, so the user can correct you if the photo misleads you.
        """;

    private static string DescribeWatch(Watch watch)
    {
        var lines = new List<string>
        {
            $"- Brand and model: {watch.Brand} {watch.Model}",
            $"- Movement: {watch.MovementType}"
        };

        void Add(string label, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value)) lines.Add($"- {label}: {value.Trim()}");
        }

        if (watch.CaseSizeMm is double caseSize) lines.Add($"- Case size: {caseSize} mm");
        Add("Case shape", watch.CaseShape);
        Add("Dial colour", watch.DialColor);
        Add("Bezel", watch.BezelType);
        Add("Crystal", watch.CrystalType);
        Add("Strap or bracelet", string.Join(' ', new[] { watch.BandColor, watch.BandType }
            .Where(part => !string.IsNullOrWhiteSpace(part))));
        Add("Water resistance", watch.WaterResistance);
        if (watch.ProductionYear is int year) lines.Add($"- Production year: {year}");
        Add("Country of origin", watch.CountryOfOrigin);
        Add("Owner's notes", Truncate(watch.Notes, 800));
        Add("Previous AI analysis of the watch", Truncate(watch.AiAnalysis, 800));

        return string.Join('\n', lines);
    }

    private static string DescribeMemory(StyleRecommendation memory, string? watchName)
    {
        var feedback = memory.WasHelpful switch
        {
            true => "the user said this one worked",
            false => "the user said this one missed",
            _ => "no feedback yet — worth asking about"
        };
        if (!string.IsNullOrWhiteSpace(memory.FeedbackNotes))
            feedback += $" (\"{memory.FeedbackNotes.Trim()}\")";

        var labels = new List<string> { memory.CreatedAt.ToString("yyyy-MM-dd") };
        if (watchName is not null) labels.Add(watchName);
        if (!string.IsNullOrWhiteSpace(memory.Occasion)) labels.Add($"occasion: {memory.Occasion}");
        if (!string.IsNullOrWhiteSpace(memory.Weather)) labels.Add($"weather: {memory.Weather}");

        return $"- [{string.Join(", ", labels)}] {memory.Summary} — {Truncate(memory.Outfit, 300)} — Feedback: {feedback}";
    }

    // --- Ollama ------------------------------------------------------------

    private async Task<(string Url, string Model)?> TryGetOllamaConfigAsync()
    {
        var url = await appSettings.GetAsync(AppSettingsService.Keys.OllamaUrl, "http://localhost:11434");
        var model = await appSettings.GetAsync(AppSettingsService.Keys.OllamaModel);
        if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(model)) return null;
        return (url, model);
    }

    private async Task<AgentReply> AskOllamaAsync(
        string ollamaUrl,
        string model,
        string systemPrompt,
        IReadOnlyList<StyleMessage> history,
        string newTurn,
        string? photo,
        CancellationToken ct)
    {
        string content;
        try
        {
            content = await SendChatAsync(ollamaUrl, model, systemPrompt, history, newTurn, photo, ct);
        }
        catch (OllamaRequestException ex) when (photo is not null)
        {
            // A text-only model refuses the picture. Advice from the fields
            // alone still beats no answer at all.
            logger.LogWarning(
                "Ollama rejected a style request carrying the watch photo, retrying without it: {Reason}", ex.Message);
            content = await SendChatAsync(ollamaUrl, model, systemPrompt, history, newTurn, null, ct);
        }

        if (string.IsNullOrWhiteSpace(content))
            throw new InvalidOperationException("The style agent got an empty reply from Ollama.");

        return ParseReply(content);
    }

    private async Task<string> SendChatAsync(
        string ollamaUrl,
        string model,
        string systemPrompt,
        IReadOnlyList<StyleMessage> history,
        string newTurn,
        string? photo,
        CancellationToken ct)
    {
        // The note goes on only when the picture actually does, so the retry
        // without it doesn't leave the model hunting for an image that is gone.
        var prompt = photo is null ? systemPrompt : $"{systemPrompt}\n\n{PhotoNote}";

        var messages = new List<object> { new { role = "system", content = prompt } };
        foreach (var message in history)
        {
            messages.Add(new
            {
                role = message.Role == StyleMessageRole.Assistant ? "assistant" : "user",
                content = message.Content
            });
        }

        // The photo rides on the newest turn rather than the first one: history
        // is replayed from the database, which stores no images, so this is what
        // keeps the watch in view on every turn of a long conversation.
        messages.Add(photo is null
            ? (object)new { role = "user", content = newTurn }
            : new { role = "user", content = newTurn, images = new[] { photo } });

        var requestBody = new
        {
            model,
            messages,
            format = "json",
            stream = false
        };

        var json = JsonSerializer.Serialize(requestBody);
        var request = new HttpRequestMessage(HttpMethod.Post, $"{ollamaUrl.TrimEnd('/')}/api/chat")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        HttpResponseMessage response;
        string responseBody;
        try
        {
            response = await httpClient.SendAsync(request, ct);
            responseBody = await response.Content.ReadAsStringAsync(ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "The style agent could not reach Ollama at {OllamaUrl}.", ollamaUrl);
            throw new InvalidOperationException("The style agent could not reach Ollama. Check the Ollama URL in Admin → Settings.");
        }

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Ollama returned {StatusCode} for a style agent request: {Body}", (int)response.StatusCode, responseBody);
            throw new OllamaRequestException($"Ollama API error: {responseBody}");
        }

        try
        {
            using var doc = JsonDocument.Parse(responseBody);
            return doc.RootElement.GetProperty("message").GetProperty("content").GetString() ?? "";
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            throw new InvalidOperationException("Ollama returned a response the style agent could not read.");
        }
    }

    /// <summary>
    /// Ollama answered but refused the request — a bad model name, say, or a
    /// text-only model handed a picture. Derives from InvalidOperationException
    /// so the controller keeps turning it into a 400 with the reason attached.
    /// </summary>
    private sealed class OllamaRequestException(string message) : InvalidOperationException(message);

    private sealed record ParsedRecommendation(string Summary, string Outfit, string? Occasion, string? Weather);

    private sealed record AgentReply(string Text, List<string> FollowUps, ParsedRecommendation? Recommendation);

    /// <summary>
    /// Reads the agent's JSON contract, falling back to treating the whole
    /// completion as prose when a model ignores it.
    /// </summary>
    private static AgentReply ParseReply(string content)
    {
        var json = OllamaJson.ExtractObject(content);
        if (json is null) return new AgentReply(content.Trim(), [], null);

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return new AgentReply(content.Trim(), [], null);

            var followUps = new List<string>();
            if (root.TryGetProperty("followUps", out var followUpsElement)
                && followUpsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in followUpsElement.EnumerateArray())
                {
                    if (item.ValueKind != JsonValueKind.String) continue;
                    var question = item.GetString()?.Trim();
                    if (string.IsNullOrEmpty(question)) continue;
                    followUps.Add(Truncate(question, 200)!);
                    if (followUps.Count == MaxFollowUps) break;
                }
            }

            var replyText = ReadString(root, "reply");

            ParsedRecommendation? recommendation = null;
            if (root.TryGetProperty("recommendation", out var recommendationElement)
                && recommendationElement.ValueKind == JsonValueKind.Object)
            {
                var outfit = ReadString(recommendationElement, "outfit") ?? replyText;
                if (outfit is not null)
                {
                    var summary = ReadString(recommendationElement, "summary") ?? FirstLine(outfit);
                    recommendation = new ParsedRecommendation(
                        Truncate(summary, 200)!,
                        outfit,
                        ReadString(recommendationElement, "occasion"),
                        ReadString(recommendationElement, "weather"));
                }
            }

            return new AgentReply(replyText ?? recommendation?.Outfit ?? content.Trim(), followUps, recommendation);
        }
        catch (JsonException)
        {
            return new AgentReply(content.Trim(), [], null);
        }
    }

    // --- Small helpers -----------------------------------------------------

    private static string? ReadString(JsonElement parent, string property) =>
        parent.TryGetProperty(property, out var element) && element.ValueKind == JsonValueKind.String
            ? Normalize(element.GetString())
            : null;

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>The user's turn as it will be stored, or null when there is nothing to send.</summary>
    private static string? BuildUserTurn(string? message, string? occasion, string? weather)
    {
        var text = Normalize(message);
        if (text is not null) return text;

        var parts = new List<string>();
        if (occasion is not null) parts.Add($"Occasion: {occasion}");
        if (weather is not null) parts.Add($"Weather: {weather}");
        return parts.Count > 0 ? string.Join(" · ", parts) : null;
    }

    private static string FirstLine(string text)
    {
        var line = text.Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim();
        return string.IsNullOrEmpty(line) ? "Outfit recommendation" : line.TrimStart('#', '*', '-', ' ');
    }

    private static string? Truncate(string? text, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var trimmed = text.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength] + "…";
    }
}
