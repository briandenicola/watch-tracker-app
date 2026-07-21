namespace WatchTracker.Api.Services;

public record WebSearchResultItem(string Title, string Description, string Url);

public interface IWebSearchClient
{
    string ProviderName { get; }
    Task<List<WebSearchResultItem>> SearchAsync(string query, CancellationToken ct = default);
}
