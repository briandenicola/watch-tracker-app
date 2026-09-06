namespace WatchTracker.Api.Services;

/// <summary>
/// Bounds what agent code writes to the log.
///
/// Debug and Trace are an explicit operator choice to see everything — prompts,
/// model replies and provider bodies included — but "everything" still has to fit
/// in a log line, and none of it is trusted: it is model, user or provider text, so
/// it is truncated and flattened before it can forge a line of its own.
/// </summary>
public static class LogText
{
    public const int DefaultMaxLength = 4000;

    /// <summary>Full text for a Debug reader, bounded and on one line.</summary>
    public static string Bounded(string? value, int maxLength = DefaultMaxLength)
    {
        if (string.IsNullOrEmpty(value)) return "";
        var flattened = value.ReplaceLineEndings(" ");
        return flattened.Length <= maxLength
            ? flattened
            : $"{flattened[..maxLength]}… ({flattened.Length} characters)";
    }

    /// <summary>
    /// A short model-chosen name — an action type, a tool, a clarification constraint —
    /// reduced to what a name can hold. Safe at any level, so a redacted log can still
    /// say what the model asked for.
    /// </summary>
    public static string Token(string? value, int maxLength = 40)
    {
        if (string.IsNullOrWhiteSpace(value)) return "none";
        var cleaned = new string(value
            .Where(c => char.IsLetterOrDigit(c) || c is '_' or '-' or ' ')
            .Take(maxLength)
            .ToArray())
            .Trim();
        return cleaned.Length == 0 ? "unprintable" : cleaned;
    }
}
