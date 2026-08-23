namespace WatchTracker.Api.Services;

/// <summary>Readable text pulled from a page the owner linked to the watch.</summary>
public sealed record LinkedPageExcerpt(string Url, string? Title, string Text);

public interface IProductPageReader
{
    /// <summary>
    /// Fetches a page and returns its text, or null when the URL cannot or
    /// should not be read. Never throws for an unreachable or hostile page —
    /// a link that will not load is a missing detail, not a failed analysis.
    /// </summary>
    Task<LinkedPageExcerpt?> ReadAsync(string url, CancellationToken ct = default);
}
