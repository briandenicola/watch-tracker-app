namespace WatchTracker.Api.Authentication;

/// <summary>
/// What an API key may do. Every key reads. "write" lets it change any data
/// over the REST API (see <see cref="ApiKeyReadOnlyMiddleware"/>); "agents" lets
/// it run the AI agents over MCP; "wishlist" lets it add a watch to the wish
/// list over MCP and nothing else, so an assistant can be given that without
/// the power to edit or delete. A write key can already add over REST, so it
/// may use the MCP tool too. Settings offers read, read,wishlist, read,agents,
/// read,agents,wishlist and read,write.
/// </summary>
public static class ApiKeyScopes
{
    public const string Read = "read";
    public const string Write = "write";
    public const string Agents = "agents";
    public const string Wishlist = "wishlist";
    public const string ClaimType = "watchtracker:api_key_scopes";

    /// <summary>Canonical form ("read", then write, agents and wishlist when given). Null when a scope is unknown.</summary>
    public static string? Normalize(string? scopes)
    {
        if (string.IsNullOrWhiteSpace(scopes)) return Read;

        var parts = scopes
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(p => p.ToLowerInvariant())
            .Distinct()
            .ToList();
        if (parts.Any(p => p is not (Read or Write or Agents or Wishlist))) return null;

        var canonical = new List<string> { Read };
        if (parts.Contains(Write)) canonical.Add(Write);
        if (parts.Contains(Agents)) canonical.Add(Agents);
        if (parts.Contains(Wishlist)) canonical.Add(Wishlist);
        return string.Join(',', canonical);
    }

    public static bool Has(string? scopes, string scope) =>
        scopes is not null
        && scopes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Contains(scope, StringComparer.OrdinalIgnoreCase);
}
