using System.ComponentModel;
using System.Security.Claims;
using System.Threading.RateLimiting;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using WatchTracker.Api.Authentication;
using WatchTracker.Api.DTOs;
using WatchTracker.Api.Models;
using WatchTracker.Api.Services;

namespace WatchTracker.Api.Mcp;

/// <summary>
/// The caller behind an MCP request: the key's owner and its scopes. Everything a
/// tool touches is scoped to <see cref="UserId"/>; no tool takes a user id.
/// </summary>
public class McpCaller(IHttpContextAccessor accessor)
{
    private ClaimsPrincipal Principal => accessor.HttpContext?.User
        ?? throw new McpException("No authenticated caller.");

    public int UserId => int.TryParse(Principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
        ? id
        : throw new McpException("No authenticated caller.");

    public bool HasScope(string scope) =>
        ApiKeyScopes.Has(Principal.FindFirstValue(ApiKeyScopes.ClaimType), scope);

    public void RequireScope(string scope)
    {
        if (!HasScope(scope))
            throw new McpException(
                $"This API key does not have the \"{scope}\" scope. Create a key with read,{scope} access.");
    }
}

/// <summary>Caps how often one user can start an agent run, since each one drives the local model.</summary>
public static class McpAgentLimiter
{
    private static readonly PartitionedRateLimiter<int> Limiter =
        PartitionedRateLimiter.Create<int, int>(userId =>
            RateLimitPartition.GetFixedWindowLimiter(userId, _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    public static void Acquire(int userId)
    {
        using var lease = Limiter.AttemptAcquire(userId);
        if (!lease.IsAcquired)
            throw new McpException("Too many agent requests. Wait a minute before trying again.");
    }
}

/// <summary>Read-only tools over the signed-in collector's own data.</summary>
[McpServerToolType]
public class WatchTrackerReadTools(
    McpCaller caller,
    IWatchCatalogService watches,
    IWatchWearLogService wearLogs,
    ICollectionProfileService profile,
    ICollectionReviewService review,
    IStyleAgentService style)
{
    private const int MaxPageSize = 50;

    [McpServerTool(Name = "list_watches", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("List watches in the active collection (not the wish list or retired watches). Optionally filter by a case-insensitive text match on brand or model.")]
    public async Task<object> ListWatches(
        [Description("Optional text to match against brand or model.")] string? query = null,
        [Description("Page size, 1-50. Default 25.")] int limit = 25,
        [Description("Number of watches to skip. Default 0.")] int offset = 0,
        CancellationToken ct = default)
    {
        var all = (await watches.GetAllAsync(caller.UserId, includeDisposed: false, ct))
            .Where(w => !w.IsWishList && w.Disposition is null);
        return Page(all, query, limit, offset);
    }

    [McpServerTool(Name = "list_wishlist", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("List wish list watches in priority order. Optionally filter by brand or model text.")]
    public async Task<object> ListWishlist(
        [Description("Optional text to match against brand or model.")] string? query = null,
        [Description("Page size, 1-50. Default 25.")] int limit = 25,
        [Description("Number of watches to skip. Default 0.")] int offset = 0,
        CancellationToken ct = default)
    {
        var all = (await watches.GetAllAsync(caller.UserId, includeDisposed: false, ct))
            .Where(w => w.IsWishList && w.Disposition is null)
            .OrderBy(w => w.WishlistPriority ?? int.MaxValue)
            .ThenBy(w => w.Id);
        return Page(all, query, limit, offset);
    }

    [McpServerTool(Name = "get_watch", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("Get every recorded detail for one watch (collection or wish list) by id.")]
    public async Task<WatchDto> GetWatch(
        [Description("The watch id from list_watches or list_wishlist.")] int id,
        CancellationToken ct = default) =>
        await watches.GetByIdAsync(id, caller.UserId, ct)
            ?? throw new McpException("Watch not found.");

    [McpServerTool(Name = "collection_profile", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("Computed collection facts: coverage, gaps, redundancy, data quality, wear and wish list overlap. These are calculated by the app, not by a model.")]
    public async Task<CollectionProfileDto> CollectionProfile(CancellationToken ct = default) =>
        await profile.GetProfileAsync(caller.UserId, ct);

    [McpServerTool(Name = "list_wear_log", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("Recent wear events, newest first.")]
    public async Task<object> ListWearLog(
        [Description("How many events to return, 1-100. Default 30.")] int limit = 30,
        CancellationToken ct = default)
    {
        var take = Math.Clamp(limit, 1, 100);
        var logs = (await wearLogs.GetWearLogsAsync(caller.UserId, ct))
            .OrderByDescending(l => l.WornDate)
            .Take(take)
            .Select(l => new
            {
                l.Id,
                l.WatchId,
                l.WatchBrand,
                l.WatchModel,
                l.WornDate,
                l.StartedAt,
                l.EndedAt,
                l.DurationMinutes
            })
            .ToList();
        return new { count = logs.Count, events = logs };
    }

    [McpServerTool(Name = "get_collection_review", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("The last stored collection review (strengths, weaknesses, recommendations, candidates) and whether it is out of date. Does not run the model; use generate_collection_review for that.")]
    public async Task<CollectionReviewStateDto> GetCollectionReview(CancellationToken ct = default) =>
        await review.GetStateAsync(caller.UserId, ct);

    [McpServerTool(Name = "get_style_memory", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("The outfits the Style Agent has recommended for one watch, with any Worked/Missed feedback.")]
    public async Task<object> GetStyleMemory(
        [Description("The watch id.")] int watchId,
        CancellationToken ct = default)
    {
        var state = await style.GetStateAsync(watchId, caller.UserId, ct)
            ?? throw new McpException("Watch not found.");
        return new { watchId, recommendations = state.Memory };
    }

    private static object Page(IEnumerable<WatchDto> source, string? query, int limit, int offset)
    {
        var filtered = string.IsNullOrWhiteSpace(query)
            ? source.ToList()
            : source.Where(w =>
                    w.Brand.Contains(query, StringComparison.OrdinalIgnoreCase)
                    || w.Model.Contains(query, StringComparison.OrdinalIgnoreCase))
                .ToList();
        var items = filtered
            .Skip(Math.Max(0, offset))
            .Take(Math.Clamp(limit, 1, MaxPageSize))
            .Select(Summarize)
            .ToList();
        return new { total = filtered.Count, offset = Math.Max(0, offset), items };
    }

    private static object Summarize(WatchDto w) => new
    {
        w.Id,
        w.Brand,
        w.Model,
        movementType = w.MovementType.ToString(),
        w.Category,
        w.CaseSizeMm,
        w.CaseMaterial,
        w.DialColor,
        w.BandType,
        w.BandColor,
        w.BezelType,
        w.WaterResistance,
        w.PurchasePrice,
        w.CurrentResaleValue,
        w.TimesWorn,
        w.LastWornDate,
        w.IsWishList,
        w.WishlistPriority,
        w.LinkUrl
    };
}

/// <summary>
/// Tools that invoke the app's AI agents. Registered for every caller but refuse
/// keys without the agents scope, and are hidden from their tool list.
/// </summary>
[McpServerToolType]
public class WatchTrackerAgentTools(
    McpCaller caller,
    ICollectionAdvisorService advisor,
    ICollectionReviewService review,
    ICollectionReviewCandidateService candidates,
    IStyleAgentService style,
    IWatchRecommendationService recommendations)
{
    [McpServerTool(Name = "ask_collection_advisor", ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Ask the Collection Advisor agent about gaps, overlap, brands, budgets or current listings. It may search the web and marketplaces. Replies carry cited sources and listing cards. Continues the user's current advisor conversation unless newConversation is true. Can take up to 90 seconds.")]
    public async Task<object> AskCollectionAdvisor(
        [Description("The question, up to 2000 characters.")] string message,
        [Description("Start a fresh conversation first. Default false.")] bool newConversation = false,
        CancellationToken ct = default)
    {
        Guard();
        if (string.IsNullOrWhiteSpace(message) || message.Length > 2000)
            throw new McpException("message must be 1-2000 characters.");

        return await Run(async () =>
        {
            var state = newConversation
                ? await advisor.StartNewSessionAsync(caller.UserId, ct)
                : await advisor.GetCurrentStateAsync(caller.UserId, ct);
            var result = await advisor.SendMessageAsync(
                state.Session.Id, caller.UserId, new SendAdvisorMessageDto { Message = message }, ct)
                ?? throw new McpException("The advisor conversation was not found.");
            var reply = result.Session.Messages.LastOrDefault(m => m.Role == AdvisorMessageRole.Assistant)
                ?? throw new McpException("The advisor did not produce a reply.");
            return (object)new
            {
                sessionId = result.Session.Id,
                reply = reply.Content,
                reply.Citations,
                reply.RecommendationCards,
                reply.FollowUps,
                reply.ToolActivity
            };
        });
    }

    [McpServerTool(Name = "generate_collection_review", ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = false)]
    [Description("Run the Collection Review agent over the collection and wish list. Replaces the stored review. Needs at least two watches. Can take up to two minutes.")]
    public async Task<CollectionReviewStateDto> GenerateCollectionReview(CancellationToken ct = default)
    {
        Guard();
        return await Run(() => review.GenerateAsync(caller.UserId, ct));
    }

    [McpServerTool(Name = "find_review_candidates", ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Run the Candidate Finder: search marketplaces for real listings that fill the gaps in the stored review. Requires generate_collection_review to have run first. Replaces the stored candidates. Can take up to three minutes.")]
    public async Task<CollectionReviewCandidatesDto> FindReviewCandidates(
        [Description("Optional maximum budget.")] decimal? budget = null,
        [Description("Three-letter currency code, required with a budget (e.g. USD).")] string? currency = null,
        CancellationToken ct = default)
    {
        Guard();
        if (budget is <= 0 or > 10_000_000)
            throw new McpException("budget must be between 1 and 10,000,000.");
        if (currency is not null && currency.Length != 3)
            throw new McpException("currency must be a three-letter code.");

        return await Run(() => candidates.GenerateAsync(
            caller.UserId, new GenerateCandidatesDto { Budget = budget, Currency = currency }, ct));
    }

    [McpServerTool(Name = "ask_style_agent", ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = false)]
    [Description("Chat with the Style Agent about an outfit for one watch. It wants the occasion and weather before it commits to advice. Outfits it recommends are remembered against the watch.")]
    public async Task<object> AskStyleAgent(
        [Description("The watch id.")] int watchId,
        [Description("What you want to say, up to 2000 characters.")] string? message = null,
        [Description("The occasion, up to 200 characters.")] string? occasion = null,
        [Description("The weather, up to 200 characters.")] string? weather = null,
        CancellationToken ct = default)
    {
        Guard();
        if (message?.Length > 2000 || occasion?.Length > 200 || weather?.Length > 200)
            throw new McpException("message is limited to 2000 characters; occasion and weather to 200.");

        return await Run(async () =>
        {
            var state = await style.SendMessageAsync(
                watchId,
                caller.UserId,
                new SendStyleMessageDto { Message = message, Occasion = occasion, Weather = weather },
                ct) ?? throw new McpException("Watch not found.");
            var reply = state.Session.Messages.LastOrDefault(m => m.Role == StyleMessageRole.Assistant);
            return (object)new
            {
                watchId,
                reply = reply?.Content,
                recommendation = reply?.Recommendation,
                state.FollowUps
            };
        });
    }

    [McpServerTool(Name = "recommend_watch_for_outfit", ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = false)]
    [Description("Ask the Outfit Recommender to pick a primary and a backup watch from the active collection for an outfit. Needs at least two watches.")]
    public async Task<WatchRecommendationDto> RecommendWatchForOutfit(
        [Description("The occasion, up to 100 characters.")] string occasion,
        [Description("Describe the outfit, up to 1500 characters.")] string outfitDescription,
        [Description("Optional colour palette, up to 200 characters.")] string? colorPalette = null,
        [Description("Optional weather, up to 200 characters.")] string? weather = null,
        [Description("Optional preferences, up to 500 characters.")] string? preferences = null,
        CancellationToken ct = default)
    {
        Guard();
        if (string.IsNullOrWhiteSpace(occasion) || occasion.Length > 100
            || string.IsNullOrWhiteSpace(outfitDescription) || outfitDescription.Length > 1500
            || colorPalette?.Length > 200 || weather?.Length > 200 || preferences?.Length > 500)
            throw new McpException("An argument is missing or longer than its limit.");

        return await Run(() => recommendations.RecommendAsync(
            new WatchRecommendationRequestDto
            {
                Occasion = occasion,
                OutfitDescription = outfitDescription,
                ColorPalette = colorPalette,
                Weather = weather,
                Preferences = preferences
            },
            caller.UserId,
            ct));
    }

    private void Guard()
    {
        caller.RequireScope(ApiKeyScopes.Agents);
        McpAgentLimiter.Acquire(caller.UserId);
    }

    // Services report user-safe reasons as InvalidOperationException (the REST
    // controllers turn them into a 400 with the message); do the same here.
    private static async Task<T> Run<T>(Func<Task<T>> action)
    {
        try
        {
            return await action();
        }
        catch (InvalidOperationException ex)
        {
            throw new McpException(ex.Message);
        }
    }
}
