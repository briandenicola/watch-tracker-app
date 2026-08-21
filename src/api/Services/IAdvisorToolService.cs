using System.Text.Json;
using WatchTracker.Api.DTOs;

namespace WatchTracker.Api.Services;

public sealed class AdvisorToolContext(int userId, CollectionProfileDto profile)
{
    public int UserId { get; } = userId;
    public CollectionProfileDto Profile { get; } = profile;
    public Dictionary<string, AdvisorCitationDto> Sources { get; } =
        new(StringComparer.Ordinal);
    public Dictionary<string, MarketplaceListingItem> Listings { get; } =
        new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, CandidateFitScoreDto> ListingScores { get; } =
        new(StringComparer.OrdinalIgnoreCase);

    public static string ListingKey(string provider, string providerItemId) =>
        $"{provider}|{providerItemId}";
}

public record AdvisorToolResult(
    string OutputJson,
    AdvisorToolActivityDto Activity);

public interface IAdvisorToolService
{
    string Instructions { get; }

    Task<AdvisorToolResult> ExecuteAsync(
        string toolName,
        JsonElement arguments,
        AdvisorToolContext context,
        CancellationToken ct = default);
}
