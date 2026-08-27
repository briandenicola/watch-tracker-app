namespace WatchTracker.Api.Services;

/// <summary>
/// The scanner's explicit allow-list. v1 intentionally reads only search
/// snippets: merchant pages are not fetched until a source is proven safe.
/// </summary>
public class SiteCatalog : ISiteCatalog
{
    public IReadOnlyList<SiteCatalogEntry> Sites { get; } =
    [
        new("Ashford", "ashford.com"),
        new("WatchMaxx", "watchmaxx.com"),
        new("Bob's Watches", "bobswatches.com"),
        new("Jomashop", "jomashop.com"),
        new("Chrono24", "chrono24.com"),
        new(
            "Bezel",
            "getbezel.com",
            IsBlocked: true,
            BlockReason: "Bezel is app-first and is not scanned in v1.")
    ];
}
