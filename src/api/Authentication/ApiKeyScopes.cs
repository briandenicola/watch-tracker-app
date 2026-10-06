namespace WatchTracker.Api.Authentication;

/// <summary>
/// What an API key may do over MCP. Scopes only narrow MCP: the REST API accepts
/// any valid key as before.
/// </summary>
public static class ApiKeyScopes
{
    public const string Read = "read";
    public const string Agents = "agents";
    public const string ClaimType = "watchtracker:api_key_scopes";

    /// <summary>Canonical form, with read always present. Null when a scope is unknown.</summary>
    public static string? Normalize(string? scopes)
    {
        if (string.IsNullOrWhiteSpace(scopes)) return Read;

        var parts = scopes
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(p => p.ToLowerInvariant())
            .Distinct()
            .ToList();
        if (parts.Any(p => p is not (Read or Agents))) return null;

        return parts.Contains(Agents) ? $"{Read},{Agents}" : Read;
    }

    public static bool Has(string? scopes, string scope) =>
        scopes is not null
        && scopes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Contains(scope, StringComparer.OrdinalIgnoreCase);
}
