namespace WatchTracker.Api.Services;

public enum WebSearchStatus
{
    Success,
    NotConfigured,
    ProviderError
}

public record WebSearchResultItem(
    string Title,
    string Description,
    string Url,
    DateTime ObservedAt);

public record WebSearchResult(
    WebSearchStatus Status,
    IReadOnlyList<WebSearchResultItem> Items,
    string? Error = null);

public interface IWebSearchClient
{
    string ProviderName { get; }
    Task<WebSearchResult> SearchAsync(string query, CancellationToken ct = default);
}
