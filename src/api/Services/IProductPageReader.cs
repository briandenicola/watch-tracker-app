namespace WatchTracker.Api.Services;

/// <summary>Bounded product-page evidence pulled from a link supplied by the owner.</summary>
public sealed record LinkedPageExcerpt(
    string Url,
    string? Title,
    string Text,
    IReadOnlyDictionary<string, string>? Metadata = null,
    IReadOnlyList<string>? JsonLd = null);

public interface IProductPageReader
{
    /// <summary>
    /// Fetches a page and returns its text, or null when the URL cannot or
    /// should not be read. Never throws for an unreachable or hostile page —
    /// a link that will not load is a missing detail, not a failed analysis.
    /// </summary>
    Task<LinkedPageExcerpt?> ReadAsync(string url, CancellationToken ct = default);
}
