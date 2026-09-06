using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WatchTracker.Api.Data;
using WatchTracker.Api.DTOs;
using WatchTracker.Api.Models;

namespace WatchTracker.Api.Services;

/// <summary>
/// Stage two of the collection review: watches you could actually buy that fill
/// the gaps stage one found.
///
/// The model drives the search — it proposes what to look for, sees what came
/// back, and may change its mind once — but it never supplies a price, a score,
/// or a listing. Every candidate is a listing a marketplace actually returned,
/// scored by the same deterministic scorer the review uses for wish list fit.
/// </summary>
public class CollectionReviewCandidateService(
    AppDbContext context,
    IAppSettingsService appSettings,
    ICollectionProfileService collectionProfile,
    IRecommendationWishlistService recommendationWishlist,
    IEnumerable<IMarketplaceSearchClient> marketplaceClients,
    HttpClient httpClient,
    ILogger<CollectionReviewCandidateService> logger) : ICollectionReviewCandidateService
{
    private const int MaxQueriesPerRound = 5;
    private const int MaxListingsPerQuery = 8;
    private const int MaxCandidates = 6;
    private const int MaxRationaleLength = 300;
    private const int MaxSearchRounds = 2;
    private const string WishlistNote = "Added from a collection review candidate.";

    /// <summary>
    /// Listings go stale within days, so anything older is dropped on read rather
    /// than shown as if it were still for sale.
    /// </summary>
    public static readonly TimeSpan FreshnessWindow = TimeSpan.FromDays(7);

    /// <summary>
    /// Two searches and up to three model calls, so the ceiling sits above the
    /// review's. Program.cs gives the HTTP client room above this.
    /// </summary>
    public static readonly TimeSpan MaxExecutionTime = TimeSpan.FromSeconds(180);

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<CollectionReviewCandidatesDto> GenerateAsync(
        int userId,
        GenerateCandidatesDto request,
        CancellationToken ct = default)
    {
        if (request.Budget is not null && string.IsNullOrWhiteSpace(request.Currency))
            throw new InvalidOperationException("A budget needs a currency to go with it.");

        var stored = await context.CollectionReviews.FirstOrDefaultAsync(r => r.UserId == userId, ct)
            ?? throw new InvalidOperationException(
                "Run a collection review first — candidates are found against the gaps it identified.");

        var facts = JsonSerializer.Deserialize<CollectionReviewFactsDto>(stored.FactsJson, JsonOptions)
            ?? throw new InvalidOperationException("The stored collection review is unreadable.");

        var (ollamaUrl, model) = await GetOllamaSettingsAsync();
        var profile = await collectionProfile.GetProfileAsync(userId, ct);

        var requestTimer = Stopwatch.StartNew();
        logger.LogDebug(
            "Candidate search starting for user {UserId} against model {OllamaModel} at {OllamaUrl} "
            + "with budget {Budget} {Currency} and {MarketplaceCount} marketplace clients.",
            userId,
            model,
            ollamaUrl,
            request.Budget,
            request.Currency,
            marketplaceClients.Count());

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(MaxExecutionTime);

        SearchOutcome outcome;
        try
        {
            outcome = await RunSearchLoopAsync(ollamaUrl, model, facts, profile, request, timeout.Token);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            logger.LogWarning(
                "A candidate search hit its {LimitSeconds}-second limit after {DurationMs} ms.",
                MaxExecutionTime.TotalSeconds,
                requestTimer.ElapsedMilliseconds);
            throw new InvalidOperationException(
                "The candidate search took too long. Try again, or use a faster Ollama model.");
        }
        catch (OperationCanceledException)
        {
            // The caller hung up. Nothing else records this, which is why an
            // abandoned request used to leave no trace at all.
            logger.LogWarning(
                "A candidate search was abandoned by the caller after {DurationMs} ms.",
                requestTimer.ElapsedMilliseconds);
            throw;
        }
        catch (InvalidOperationException ex)
        {
            // The call sites above name what failed; this line times it. The exception
            // itself is Debug-only because its message can carry provider text.
            logger.LogWarning(
                "A candidate search failed after {DurationMs} ms.",
                requestTimer.ElapsedMilliseconds);
            logger.LogDebug(ex, "Candidate search for user {UserId} failed.", userId);
            throw;
        }

        logger.LogInformation(
            "A candidate search completed in {DurationMs} ms with {CandidateCount} candidates. "
            + "Marketplace status: {MarketplaceStatus}.",
            requestTimer.ElapsedMilliseconds,
            outcome.Candidates.Count,
            string.Join(", ", outcome.ProviderStatus.Select(p => $"{p.Provider}={p.Status}")));

        var candidates = outcome.Candidates.Take(MaxCandidates).ToList();
        stored.CandidatesJson = JsonSerializer.Serialize(candidates, JsonOptions);
        stored.MarketplaceStatusJson = JsonSerializer.Serialize(outcome.ProviderStatus, JsonOptions);
        stored.CandidatesGeneratedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(ct);

        return Read(stored);
    }

    public async Task<AdvisorWishlistActionResultDto?> AddToWishlistAsync(
        int userId,
        CandidateWishlistActionDto request,
        CancellationToken ct = default)
    {
        var stored = await context.CollectionReviews
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.UserId == userId, ct);
        if (stored?.CandidatesJson is null) return null;

        // Only a stored candidate may be added: the provider and item id come from
        // the client, and nothing else vouches for them.
        var card = Deserialize(stored.CandidatesJson).FirstOrDefault(c =>
            string.Equals(c.Provider, request.Provider, StringComparison.OrdinalIgnoreCase)
            && string.Equals(c.ProviderItemId, request.ProviderItemId, StringComparison.OrdinalIgnoreCase));
        if (card is null) return null;

        return await recommendationWishlist.AddAsync(card, userId, WishlistNote, ct);
    }

    /// <summary>
    /// Read the stored candidates, dropping anything that has gone stale. Stage
    /// one's report holds its numbers forever; a listing does not.
    /// </summary>
    public static CollectionReviewCandidatesDto Read(CollectionReview stored)
    {
        if (stored.CandidatesJson is null)
            return new CollectionReviewCandidatesDto
            {
                MarketplaceStatus = ReadStatus(stored.MarketplaceStatusJson),
                GeneratedAt = stored.CandidatesGeneratedAt
            };

        var all = Deserialize(stored.CandidatesJson);
        var cutoff = DateTime.UtcNow - FreshnessWindow;
        var fresh = all.Where(c => c.ObservedAt is null || c.ObservedAt >= cutoff).ToList();

        return new CollectionReviewCandidatesDto
        {
            Candidates = fresh,
            MarketplaceStatus = ReadStatus(stored.MarketplaceStatusJson),
            GeneratedAt = stored.CandidatesGeneratedAt,
            DroppedStaleListings = fresh.Count < all.Count
        };
    }

    private async Task<SearchOutcome> RunSearchLoopAsync(
        string ollamaUrl,
        string model,
        CollectionReviewFactsDto facts,
        CollectionProfileDto profile,
        GenerateCandidatesDto request,
        CancellationToken ct)
    {
        var seen = new Dictionary<string, ScoredListing>(StringComparer.OrdinalIgnoreCase);
        var providerStatus = new Dictionary<string, MarketplaceProviderStatusDto>(StringComparer.OrdinalIgnoreCase);
        var history = new StringBuilder();
        history.AppendLine(BuildOpeningPrompt(facts, request));

        for (var round = 1; round <= MaxSearchRounds; round++)
        {
            var reply = await CallModelAsync(ollamaUrl, model, history.ToString(), ct);
            var queries = ReadQueries(reply);
            var selection = ReadSelection(reply);
            logger.LogDebug(
                "Candidate search round {Round} proposed {QueryCount} queries and selected {SelectionCount} listings "
                + "from {SeenCount} seen so far.",
                round,
                queries.Count,
                selection.Count,
                seen.Count);

            // A selection ends the loop, and the last round must produce one.
            if (queries.Count == 0 || (selection.Count > 0 && round > 1))
            {
                if (queries.Count == 0 && selection.Count == 0)
                    logger.LogWarning(
                        "The candidate search model returned neither queries nor a selection in round {Round}, "
                        + "so no candidates can be shown. Reply keys: {ReplyKeys}.",
                        round,
                        DescribeKeys(reply));
                return Finish(selection, seen, providerStatus);
            }

            foreach (var query in queries)
                await SearchAsync(query, profile, request, seen, providerStatus, ct);

            history.AppendLine();
            history.AppendLine(round < MaxSearchRounds
                ? BuildResultsPrompt(seen.Values, canRefine: true)
                : BuildResultsPrompt(seen.Values, canRefine: false));
        }

        var final = await CallModelAsync(ollamaUrl, model, history.ToString(), ct);
        var finalSelection = ReadSelection(final);
        logger.LogDebug(
            "Candidate search final round selected {SelectionCount} of {SeenCount} listings.",
            finalSelection.Count,
            seen.Count);
        if (finalSelection.Count == 0)
            logger.LogWarning(
                "The candidate search model selected nothing from {SeenCount} listings. Reply keys: {ReplyKeys}.",
                seen.Count,
                DescribeKeys(final));
        return Finish(finalSelection, seen, providerStatus);
    }

    private SearchOutcome Finish(
        IReadOnlyList<Selected> selection,
        Dictionary<string, ScoredListing> seen,
        Dictionary<string, MarketplaceProviderStatusDto> providerStatus)
    {
        var cards = new List<AdvisorRecommendationCardDto>();
        foreach (var choice in selection)
        {
            // A listing the model did not see in a result cannot become a card, so
            // an invented watch has nowhere to render.
            if (!seen.TryGetValue(ListingKey(choice.Provider, choice.ProviderItemId), out var scored))
            {
                logger.LogWarning(
                    "The candidate search selected a listing no marketplace returned: {Provider} {ItemId}.",
                    choice.Provider,
                    choice.ProviderItemId);
                continue;
            }
            if (cards.Any(c => string.Equals(c.ProviderItemId, scored.Listing.ProviderItemId, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(c.Provider, scored.Listing.Provider, StringComparison.OrdinalIgnoreCase)))
                continue;

            cards.Add(ToCard(scored, choice.Rationale));
        }

        return new SearchOutcome(cards, providerStatus.Values.ToList());
    }

    private async Task SearchAsync(
        SearchQuery query,
        CollectionProfileDto profile,
        GenerateCandidatesDto request,
        Dictionary<string, ScoredListing> seen,
        Dictionary<string, MarketplaceProviderStatusDto> providerStatus,
        CancellationToken ct)
    {
        foreach (var client in marketplaceClients)
        {
            var result = await client.SearchAsync($"{query.Brand} {query.Model} watch", ct);
            providerStatus[client.ProviderName] = new MarketplaceProviderStatusDto
            {
                Provider = client.ProviderName,
                Status = result.Status.ToString(),
                Error = result.Error
            };
            if (result.Status != MarketplaceSearchStatus.Success)
            {
                logger.LogWarning(
                    "Marketplace {Provider} returned {Status} for a candidate query: {ProviderError}",
                    client.ProviderName,
                    result.Status,
                    result.Error ?? "no detail given");
                logger.LogDebug(
                    "The failed {Provider} query was for {Brand} {Model}.",
                    client.ProviderName,
                    query.Brand,
                    query.Model);
                continue;
            }

            // Marketplace results are noisy: straps, boxes, and lookalikes come
            // back for any query, so a title that does not name the watch is out.
            var matching = result.Listings
                .Where(l => l.ListingType == MarketplaceListingType.FixedPrice)
                .Where(l => request.Currency is null
                    || l.Currency.Equals(request.Currency, StringComparison.OrdinalIgnoreCase))
                .Where(l => l.Title.Contains(query.Brand, StringComparison.OrdinalIgnoreCase)
                    && l.Title.Contains(query.Model, StringComparison.OrdinalIgnoreCase))
                .Take(MaxListingsPerQuery)
                .ToList();

            // Nothing downstream can tell "the provider found nothing" apart from
            // "the filters dropped everything it found", and they need different fixes.
            logger.LogDebug(
                "Marketplace {Provider} returned {ListingCount} listings for {Brand} {Model}; "
                + "{MatchCount} survived the fixed-price, currency and title filters.",
                client.ProviderName,
                result.Listings.Count,
                query.Brand,
                query.Model,
                matching.Count);

            foreach (var listing in matching)
            {
                var key = ListingKey(listing.Provider, listing.ProviderItemId);
                if (seen.ContainsKey(key)) continue;

                var budget = request.Budget is not null
                    && listing.TotalPrice is not null
                    && listing.Currency.Equals(request.Currency, StringComparison.OrdinalIgnoreCase)
                        ? request.Budget
                        : null;
                var score = collectionProfile.ScoreCandidate(
                    profile,
                    new CollectionCandidateProfile
                    {
                        Brand = listing.Brand ?? query.Brand,
                        Model = listing.Model ?? query.Model,
                        MovementType = listing.MovementType,
                        CaseSizeMm = listing.CaseSizeMm,
                        DialColor = listing.DialColor,
                        BandType = listing.BandType,
                        Price = listing.TotalPrice
                    },
                    budget);
                seen[key] = new ScoredListing(listing, score);
            }
        }
    }

    private static string BuildOpeningPrompt(CollectionReviewFactsDto facts, GenerateCandidatesDto request)
    {
        var prompt = new StringBuilder();
        prompt.AppendLine(OpeningInstructions);
        if (request.Budget is not null)
            prompt.AppendLine($"Budget: up to {request.Budget} {request.Currency}, delivered.");
        prompt.AppendLine();
        prompt.AppendLine("Gaps and coverage already computed from the collection and wish list:");
        prompt.AppendLine(JsonSerializer.Serialize(
            new
            {
                collection = Summarize(facts.Collection),
                wishlist = Summarize(facts.Wishlist),
                combined = Summarize(facts.Combined),
                owned = facts.CollectionWatches,
                wanted = facts.WishlistWatches
            },
            JsonOptions));
        return prompt.ToString();
    }

    private static object Summarize(CollectionSetStatsDto stats) => new
    {
        stats.Label,
        stats.WatchCount,
        coverage = stats.Coverage.Select(c => new
        {
            c.Dimension,
            values = c.Values.Select(v => new { v.Value, v.Count })
        }),
        gaps = stats.Gaps.Select(g => new { g.Summary, g.Reason }),
        redundancies = stats.Redundancies.Select(r => new { r.Summary, r.Reason })
    };

    private const string OpeningInstructions = """
        You are finding watches a collector could buy that fill the gaps in the
        collection and wish list described below. The gaps and counts were computed
        from the database. Treat them as settled fact.

        Propose 3 to 5 searches for specific watches that would fill those gaps.
        Name real models, not categories: "Tudor Black Bay 36", not "a smaller
        diver". Do not propose a watch already owned or already wanted.

        Reply with JSON only:

        {"queries": [{"brand": "Tudor", "model": "Black Bay 36"}]}
        """;

    private static string BuildResultsPrompt(IEnumerable<ScoredListing> seen, bool canRefine)
    {
        var prompt = new StringBuilder();
        prompt.AppendLine("These listings came back. The fit scores were computed against the collection, not by you.");
        prompt.AppendLine(JsonSerializer.Serialize(
            seen.Select(s => new
            {
                s.Listing.Provider,
                s.Listing.ProviderItemId,
                s.Listing.Title,
                price = s.Listing.TotalPrice ?? s.Listing.Price,
                s.Listing.Currency,
                s.Listing.Condition,
                fitScore = s.Score.TotalScore,
                reasons = s.Score.Reasons
            }),
            JsonOptions));
        prompt.AppendLine();
        prompt.AppendLine(canRefine ? RefineInstructions : SelectInstructions);
        return prompt.ToString();
    }

    private const string RefineInstructions = """
        Either pick the ones worth showing, or search again if these missed what
        the gaps called for — if everything came back too large, or nothing matched
        the model you meant, propose different searches.

        To search again, reply with JSON only:

        {"queries": [{"brand": "Cartier", "model": "Tank Must"}]}

        To pick, reply with JSON only. Use only provider and providerItemId values
        that appear above, and say in one sentence what each one does for this
        collection:

        {"candidates": [{"provider": "eBay", "providerItemId": "123", "rationale": "one sentence"}]}
        """;

    private const string SelectInstructions = """
        Pick the ones worth showing — at most six, best first. Use only provider and
        providerItemId values that appear above, and say in one sentence what each
        one does for this collection. Do not quote a price or a score; those are
        already known.

        Reply with JSON only:

        {"candidates": [{"provider": "eBay", "providerItemId": "123", "rationale": "one sentence"}]}
        """;

    private async Task<JsonElement> CallModelAsync(
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

        // Left unhandled, an unreachable Ollama surfaced as a 500 with no actionable
        // message, which is what "unable to search for candidates right now" looked like.
        var result = await OllamaChat.SendAsync(
            httpClient,
            request,
            logger,
            "candidate search",
            ollamaUrl,
            prompt,
            ct);
        if (!result.IsSuccess)
            throw new InvalidOperationException($"Ollama API error: {result.Body}");

        using var envelope = JsonDocument.Parse(result.Body);
        var content = envelope.RootElement.GetProperty("message").GetProperty("content").GetString()
            ?? throw new InvalidOperationException("No content in the Ollama response.");

        var json = OllamaJson.ExtractObject(content);
        if (json is null)
        {
            logger.LogWarning(
                "The candidate search reply held no JSON object in {ReplyLength} characters.",
                content.Length);
            logger.LogDebug("The candidate search reply without JSON was: {ModelReply}", LogText.Bounded(content));
            throw new InvalidOperationException("The candidate search did not return usable JSON.");
        }
        try
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.Clone();
        }
        catch (JsonException ex)
        {
            logger.LogWarning(
                "The candidate search returned malformed JSON in {ReplyLength} characters.",
                json.Length);
            logger.LogDebug(ex, "The unparsable candidate search reply was: {ModelReply}", LogText.Bounded(json));
            throw new InvalidOperationException("The candidate search returned malformed JSON.");
        }
    }

    /// <summary>
    /// The property names a reply carried, so an off-contract shape is visible without
    /// putting the reply itself — which is derived from the user's collection — in the
    /// log. Names are bounded and stripped so they cannot forge a log line.
    /// </summary>
    private static string DescribeKeys(JsonElement reply) =>
        reply.ValueKind == JsonValueKind.Object
            ? string.Join(", ", reply.EnumerateObject().Select(p => LogText.Token(p.Name)).Take(10))
            : reply.ValueKind.ToString();

    private static List<SearchQuery> ReadQueries(JsonElement root)
    {
        if (!root.TryGetProperty("queries", out var array) || array.ValueKind != JsonValueKind.Array)
            return [];

        return array.EnumerateArray()
            .Where(q => q.ValueKind == JsonValueKind.Object)
            .Select(q => new SearchQuery(ReadString(q, "brand") ?? "", ReadString(q, "model") ?? ""))
            .Where(q => q.Brand.Length > 0 && q.Model.Length > 0)
            .Take(MaxQueriesPerRound)
            .ToList();
    }

    private static List<Selected> ReadSelection(JsonElement root)
    {
        if (!root.TryGetProperty("candidates", out var array) || array.ValueKind != JsonValueKind.Array)
            return [];

        return array.EnumerateArray()
            .Where(c => c.ValueKind == JsonValueKind.Object)
            .Select(c => new Selected(
                ReadString(c, "provider") ?? "",
                ReadString(c, "providerItemId") ?? "",
                Truncate(ReadString(c, "rationale"), MaxRationaleLength)))
            .Where(c => c.Provider.Length > 0 && c.ProviderItemId.Length > 0)
            .Take(MaxCandidates)
            .ToList();
    }

    private static AdvisorRecommendationCardDto ToCard(ScoredListing scored, string? rationale)
    {
        var listing = scored.Listing;
        var reasons = new List<string>();
        if (!string.IsNullOrWhiteSpace(rationale)) reasons.Add(rationale);
        reasons.AddRange(scored.Score.Reasons);

        return new AdvisorRecommendationCardDto
        {
            Provider = listing.Provider,
            ProviderItemId = listing.ProviderItemId,
            Title = listing.Title,
            ItemUrl = listing.ItemUrl,
            ImageUrl = listing.ImageUrl,
            Price = listing.Price,
            ShippingPrice = listing.ShippingPrice,
            TotalPrice = listing.TotalPrice,
            Currency = listing.Currency,
            Condition = listing.Condition,
            Brand = listing.Brand,
            Model = listing.Model,
            ReferenceNumber = listing.ReferenceNumber,
            ObservedAt = listing.ObservedAt,
            FitScore = scored.Score.TotalScore,
            Reasons = reasons
        };
    }

    private async Task<(string Url, string Model)> GetOllamaSettingsAsync()
    {
        var url = await appSettings.GetAsync(AppSettingsService.Keys.OllamaUrl, "http://localhost:11434");
        var model = await appSettings.GetAsync(AppSettingsService.Keys.OllamaModel);
        if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(model))
            throw new InvalidOperationException(
                "Finding candidates needs Ollama. Set the Ollama URL and model under Admin -> Settings.");
        return (url, model);
    }

    private static List<AdvisorRecommendationCardDto> Deserialize(string json) =>
        JsonSerializer.Deserialize<List<AdvisorRecommendationCardDto>>(json, JsonOptions) ?? [];

    private static List<MarketplaceProviderStatusDto> ReadStatus(string? json) =>
        json is null
            ? []
            : JsonSerializer.Deserialize<List<MarketplaceProviderStatusDto>>(json, JsonOptions) ?? [];

    private static string ListingKey(string provider, string providerItemId) =>
        $"{provider}|{providerItemId}";

    private static string? ReadString(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()?.Trim()
            : null;

    private static string? Truncate(string? value, int maxLength) =>
        string.IsNullOrWhiteSpace(value) ? null
        : value.Length <= maxLength ? value
        : value[..maxLength];

    private sealed record SearchQuery(string Brand, string Model);

    private sealed record Selected(string Provider, string ProviderItemId, string? Rationale);

    private sealed record ScoredListing(MarketplaceListingItem Listing, CandidateFitScoreDto Score);

    private sealed record SearchOutcome(
        List<AdvisorRecommendationCardDto> Candidates,
        List<MarketplaceProviderStatusDto> ProviderStatus);
}
