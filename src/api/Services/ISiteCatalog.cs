namespace WatchTracker.Api.Services;

public record SiteCatalogEntry(
    string Name,
    string Domain,
    bool IsBlocked = false,
    string? BlockReason = null,
    bool AllowDirectFetch = false);

public interface ISiteCatalog
{
    IReadOnlyList<SiteCatalogEntry> Sites { get; }
}
