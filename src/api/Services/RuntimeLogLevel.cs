namespace WatchTracker.Api.Services;

/// <summary>
/// Applies the admin-chosen log level to the running application.
///
/// The stored setting used to move only the default category, so turning logging
/// up to Debug left <c>Microsoft.AspNetCore</c> pinned at Warning by
/// appsettings.json — and that is the category that records a request the caller
/// abandoned or the rate limiter rejected, which is exactly what an agent request
/// that "fails with nothing in the log" looks like.
/// </summary>
public static class RuntimeLogLevel
{
    public const string DefaultKey = "Logging:LogLevel:Default";
    public const string AspNetCoreKey = "Logging:LogLevel:Microsoft.AspNetCore";

    public const string Fallback = "Information";

    /// <summary>
    /// Framework logs are noisy, so they follow the setting only once someone has
    /// deliberately asked for detail; otherwise they stay at Warning.
    /// </summary>
    public static void Apply(DynamicConfigurationProvider provider, string? level)
    {
        var value = string.IsNullOrWhiteSpace(level) ? Fallback : level.Trim();
        provider.Set(DefaultKey, value);
        provider.Set(AspNetCoreKey, IsVerbose(value) ? value : "Warning");
    }

    private static bool IsVerbose(string level) =>
        level.Equals("Trace", StringComparison.OrdinalIgnoreCase)
        || level.Equals("Debug", StringComparison.OrdinalIgnoreCase);
}
