namespace WatchTracker.Api.Authentication;

/// <summary>
/// What an API key may do. Every key reads. "write" lets it change data over
/// the REST API (see <see cref="ApiKeyReadOnlyMiddleware"/>); "agents" lets it
/// run the AI agents over MCP. Settings offers read, read,write and
/// read,agents.
/// </summary>
public static class ApiKeyScopes
{
    public const string Read = "read";
    public const string Write = "write";
    public const string Agents = "agents";
    public const string ClaimType = "watchtracker:api_key_scopes";

    /// <summary>Canonical form ("read", then write and agents when given). Null when a scope is unknown.</summary>
    public static string? Normalize(string? scopes)
    {
        if (string.IsNullOrWhiteSpace(scopes)) return Read;

        var parts = scopes
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(p => p.ToLowerInvariant())
            .Distinct()
            .ToList();
        if (parts.Any(p => p is not (Read or Write or Agents))) return null;

        var canonical = new List<string> { Read };
        if (parts.Contains(Write)) canonical.Add(Write);
        if (parts.Contains(Agents)) canonical.Add(Agents);
        return string.Join(',', canonical);
    }

    public static bool Has(string? scopes, string scope) =>
        scopes is not null
        && scopes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Contains(scope, StringComparer.OrdinalIgnoreCase);
}
