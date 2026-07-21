namespace WatchTracker.Api.Services;

public record BraveSearchResultItem(string Title, string Description, string Url);

public interface IBraveSearchClient
{
    Task<List<BraveSearchResultItem>> SearchAsync(string query, CancellationToken ct = default);
}
