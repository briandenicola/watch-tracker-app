namespace WatchTracker.Api.Services;

/// <summary>
/// Helpers for reading JSON back out of an Ollama completion, which — even when
/// asked for JSON and nothing else — often arrives wrapped in a code fence or
/// padded with a sentence of preamble.
/// </summary>
public static class OllamaJson
{
    /// <summary>
    /// The first balanced JSON object in <paramref name="content"/>, or null when
    /// there isn't one.
    /// </summary>
    public static string? ExtractObject(string content)
    {
        var text = content.Trim();
        if (text.StartsWith("```"))
        {
            var firstNewline = text.IndexOf('\n');
            if (firstNewline >= 0) text = text[(firstNewline + 1)..];
            var fenceEnd = text.LastIndexOf("```", StringComparison.Ordinal);
            if (fenceEnd >= 0) text = text[..fenceEnd];
        }

        var start = text.IndexOf('{');
        if (start < 0) return null;

        var depth = 0;
        var inString = false;
        var escaped = false;
        for (var i = start; i < text.Length; i++)
        {
            var c = text[i];

            if (inString)
            {
                if (escaped) escaped = false;
                else if (c == '\\') escaped = true;
                else if (c == '"') inString = false;
                continue;
            }

            if (c == '"') inString = true;
            else if (c == '{') depth++;
            else if (c == '}')
            {
                depth--;
                if (depth == 0) return text[start..(i + 1)];
            }
        }

        return null;
    }
}
