using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WatchTracker.Api.Data;
using WatchTracker.Api.DTOs;
using WatchTracker.Api.Models;

namespace WatchTracker.Api.Services;

public class CollectionReviewService(
    AppDbContext context,
    IAppSettingsService appSettings,
    ICollectionProfileService collectionProfile,
    HttpClient httpClient,
    ILogger<CollectionReviewService> logger) : ICollectionReviewService
{
    // A review of one or zero watches has nothing to compare against.
    private const int MinimumWatches = 2;
    private const string NotConfiguredHint =
        "The collection review needs Ollama. Set the Ollama URL and model under Admin -> Settings.";
    private const int MaxFindingsPerSection = 6;
    private const int MaxSummaryLength = 600;
    private const int MaxFindingSummaryLength = 160;
    private const int MaxFindingDetailLength = 1200;

    /// <summary>
    /// How long a review may take before it is abandoned. The HTTP client is
    /// given room above this in Program.cs so that this ceiling is what stops a
    /// slow model, rather than the client's own default cutting in first.
    /// </summary>
    public static readonly TimeSpan MaxExecutionTime = TimeSpan.FromSeconds(120);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<CollectionReviewStateDto> GetStateAsync(int userId, CancellationToken ct = default)
    {
        var stored = await context.CollectionReviews
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.UserId == userId, ct);

        CollectionReviewDto? review = null;
        if (stored is not null)
        {
            review = Deserialize(stored);
            review.IsStale = await IsStaleAsync(stored, userId, ct);
        }

        return await BuildStateAsync(review);
    }

    public async Task<CollectionReviewStateDto> GenerateAsync(int userId, CancellationToken ct = default)
    {
        var facts = await collectionProfile.GetReviewFactsAsync(userId, ct);
        var totalWatches = facts.Collection.WatchCount + facts.Wishlist.WatchCount;
        if (totalWatches < MinimumWatches)
            throw new InvalidOperationException(
                $"Add at least {MinimumWatches} watches to your collection or wish list before running a review.");

        var (ollamaUrl, model) = await GetOllamaSettingsAsync(ct);

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(MaxExecutionTime);

        string content;
        try
        {
            content = await CallModelAsync(ollamaUrl, model, BuildPrompt(facts), timeout.Token);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new InvalidOperationException(
                "The collection review took too long to generate. Try again, or use a faster Ollama model.");
        }

        var parsed = Parse(content, facts);
        var stored = await PersistAsync(userId, parsed, facts, ct);

        var dto = Deserialize(stored);
        dto.IsStale = false;
        return await BuildStateAsync(dto);
    }

    private async Task<CollectionReviewStateDto> BuildStateAsync(CollectionReviewDto? review)
    {
        var configured = await IsConfiguredAsync();
        return new CollectionReviewStateDto
        {
            Configured = configured,
            ConfigurationHint = configured ? null : NotConfiguredHint,
            Review = review
        };
    }

    private async Task<bool> IsConfiguredAsync()
    {
        var url = await appSettings.GetAsync(AppSettingsService.Keys.OllamaUrl, "http://localhost:11434");
        var model = await appSettings.GetAsync(AppSettingsService.Keys.OllamaModel);
        return !string.IsNullOrWhiteSpace(url) && !string.IsNullOrWhiteSpace(model);
    }

    private async Task<(string Url, string Model)> GetOllamaSettingsAsync(CancellationToken ct)
    {
        var url = await appSettings.GetAsync(AppSettingsService.Keys.OllamaUrl, "http://localhost:11434");
        var model = await appSettings.GetAsync(AppSettingsService.Keys.OllamaModel);
        if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(model))
            throw new InvalidOperationException(NotConfiguredHint);
        return (url, model);
    }

    private static string BuildPrompt(CollectionReviewFactsDto facts)
    {
        var prompt = new StringBuilder();
        prompt.AppendLine(PromptInstructions);
        prompt.AppendLine();
        prompt.AppendLine("Precomputed data:");
        prompt.AppendLine(JsonSerializer.Serialize(facts, JsonOptions));
        return prompt.ToString();
    }

    private const string PromptInstructions = """
        You are reviewing a watch collector's collection and wish list together.

        Every count, percentage, coverage breakdown, redundancy cluster, gap, and
        fit score below was computed from the database before you were called.
        Treat them as settled fact. Do not recount anything, do not contradict a
        number, and do not invent statistics that are not here.

        Write three sections:

        - strengths: what this collection genuinely does well. Be specific about
          which watches earn the praise.
        - weaknesses: redundancy, over-concentration, and wish list items that
          repeat what is already owned. Where a weakness is really just missing
          data, say so rather than treating it as a flaw in the collection.
        - recommendations: what is missing and worth adding. Judge the wish list
          in the context of what is already owned - a wish list full of dress
          watches is filling a gap, not repeating one, if the collection is
          entirely sport watches. Say which wish list items are worth keeping and
          which are redundant.

        Each entry cites the watch ids it is about in watchIds. Only use ids that
        appear in the data below. An entry about the collection as a whole may use
        an empty list.

        Reply with JSON only, in exactly this shape:

        {
          "summary": "two or three sentences on the collection overall",
          "strengths": [{"summary": "short label", "detail": "the reasoning", "watchIds": [1, 2]}],
          "weaknesses": [{"summary": "short label", "detail": "the reasoning", "watchIds": []}],
          "recommendations": [{"summary": "short label", "detail": "the reasoning", "watchIds": []}]
        }
        """;

    private async Task<string> CallModelAsync(
        string ollamaUrl,
        string model,
        string prompt,
        CancellationToken ct)
    {
        var requestBody = new
        {
            model,
            messages = new[] { new { role = "user", content = prompt } },
            format = "json",
            stream = false
        };

        var request = new HttpRequestMessage(HttpMethod.Post, $"{ollamaUrl.TrimEnd('/')}/api/chat")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json")
        };

        var response = await httpClient.SendAsync(request, ct);
        var responseBody = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Ollama API error: {responseBody}");

        using var document = JsonDocument.Parse(responseBody);
        return document.RootElement.GetProperty("message").GetProperty("content").GetString()
            ?? throw new InvalidOperationException("No content in the Ollama response.");
    }

    private ParsedReview Parse(string content, CollectionReviewFactsDto facts)
    {
        var json = OllamaJson.ExtractObject(content)
            ?? throw new InvalidOperationException("The collection review did not return usable JSON.");

        JsonElement root;
        try
        {
            using var document = JsonDocument.Parse(json);
            root = document.RootElement.Clone();
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "The collection review returned malformed JSON.");
            throw new InvalidOperationException("The collection review returned malformed JSON.");
        }

        // Only ids the user actually owns or wants may be cited. A hallucinated
        // id would render as a link to a watch that is not theirs, or to nothing.
        var knownIds = facts.CollectionWatches.Select(w => w.Id)
            .Concat(facts.WishlistWatches.Select(w => w.Id))
            .ToHashSet();

        var review = new ParsedReview(
            Truncate(ReadString(root, "summary"), MaxSummaryLength),
            ReadFindings(root, "strengths", knownIds),
            ReadFindings(root, "weaknesses", knownIds),
            ReadFindings(root, "recommendations", knownIds));

        if (review.Strengths.Count == 0
            && review.Weaknesses.Count == 0
            && review.Recommendations.Count == 0)
            throw new InvalidOperationException("The collection review came back empty.");

        return review;
    }

    private static List<CollectionReviewFindingDto> ReadFindings(
        JsonElement root,
        string property,
        HashSet<int> knownIds)
    {
        if (!root.TryGetProperty(property, out var array) || array.ValueKind != JsonValueKind.Array)
            return [];

        var findings = new List<CollectionReviewFindingDto>();
        foreach (var element in array.EnumerateArray())
        {
            if (element.ValueKind != JsonValueKind.Object) continue;

            var summary = Truncate(ReadString(element, "summary"), MaxFindingSummaryLength);
            var detail = Truncate(ReadString(element, "detail"), MaxFindingDetailLength);
            if (string.IsNullOrWhiteSpace(summary) && string.IsNullOrWhiteSpace(detail)) continue;

            findings.Add(new CollectionReviewFindingDto
            {
                Summary = string.IsNullOrWhiteSpace(summary) ? "Observation" : summary,
                Detail = detail ?? string.Empty,
                WatchIds = ReadWatchIds(element, knownIds)
            });

            if (findings.Count == MaxFindingsPerSection) break;
        }

        return findings;
    }

    private static List<int> ReadWatchIds(JsonElement element, HashSet<int> knownIds)
    {
        if (!element.TryGetProperty("watchIds", out var ids) || ids.ValueKind != JsonValueKind.Array)
            return [];

        return ids.EnumerateArray()
            .Where(id => id.ValueKind == JsonValueKind.Number)
            .Select(id => id.TryGetInt32(out var value) ? value : (int?)null)
            .Where(id => id is not null && knownIds.Contains(id.Value))
            .Select(id => id!.Value)
            .Distinct()
            .ToList();
    }

    private static string? ReadString(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()?.Trim()
            : null;

    private static string? Truncate(string? value, int maxLength) =>
        string.IsNullOrWhiteSpace(value) ? null
        : value.Length <= maxLength ? value
        : value[..maxLength];

    private async Task<CollectionReview> PersistAsync(
        int userId,
        ParsedReview review,
        CollectionReviewFactsDto facts,
        CancellationToken ct)
    {
        var stored = await context.CollectionReviews.FirstOrDefaultAsync(r => r.UserId == userId, ct);
        if (stored is null)
        {
            stored = new CollectionReview
            {
                UserId = userId,
                StrengthsJson = "[]",
                WeaknessesJson = "[]",
                RecommendationsJson = "[]",
                FactsJson = "{}"
            };
            context.CollectionReviews.Add(stored);
        }

        stored.Summary = review.Summary;
        stored.StrengthsJson = JsonSerializer.Serialize(review.Strengths, JsonOptions);
        stored.WeaknessesJson = JsonSerializer.Serialize(review.Weaknesses, JsonOptions);
        stored.RecommendationsJson = JsonSerializer.Serialize(review.Recommendations, JsonOptions);
        stored.FactsJson = JsonSerializer.Serialize(facts, JsonOptions);
        stored.CollectionWatchCount = facts.Collection.WatchCount;
        stored.WishlistWatchCount = facts.Wishlist.WatchCount;
        stored.WatchesUpdatedAt = await LatestWatchChangeAsync(userId, ct);
        stored.GeneratedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);
        return stored;
    }

    private async Task<bool> IsStaleAsync(CollectionReview stored, int userId, CancellationToken ct)
    {
        var counts = await context.Watches
            .Where(w => w.UserId == userId && (w.IsWishList || w.Disposition == null))
            .GroupBy(w => w.IsWishList)
            .Select(g => new { IsWishList = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var collectionCount = counts.FirstOrDefault(c => !c.IsWishList)?.Count ?? 0;
        var wishlistCount = counts.FirstOrDefault(c => c.IsWishList)?.Count ?? 0;
        if (collectionCount != stored.CollectionWatchCount || wishlistCount != stored.WishlistWatchCount)
            return true;

        var latestChange = await LatestWatchChangeAsync(userId, ct);
        return latestChange > stored.WatchesUpdatedAt;
    }

    private async Task<DateTime?> LatestWatchChangeAsync(int userId, CancellationToken ct) =>
        await context.Watches
            .Where(w => w.UserId == userId && (w.IsWishList || w.Disposition == null))
            .MaxAsync(w => (DateTime?)w.UpdatedAt, ct);

    private static CollectionReviewDto Deserialize(CollectionReview stored) => new()
    {
        Summary = stored.Summary,
        Strengths = DeserializeFindings(stored.StrengthsJson),
        Weaknesses = DeserializeFindings(stored.WeaknessesJson),
        Recommendations = DeserializeFindings(stored.RecommendationsJson),
        Facts = JsonSerializer.Deserialize<CollectionReviewFactsDto>(stored.FactsJson, JsonOptions) ?? new(),
        GeneratedAt = stored.GeneratedAt
    };

    private static List<CollectionReviewFindingDto> DeserializeFindings(string json) =>
        JsonSerializer.Deserialize<List<CollectionReviewFindingDto>>(json, JsonOptions) ?? [];

    private sealed record ParsedReview(
        string? Summary,
        List<CollectionReviewFindingDto> Strengths,
        List<CollectionReviewFindingDto> Weaknesses,
        List<CollectionReviewFindingDto> Recommendations);
}
