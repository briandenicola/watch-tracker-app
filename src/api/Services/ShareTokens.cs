using System.Security.Cryptography;
using Microsoft.AspNetCore.WebUtilities;

namespace WatchTracker.Api.Services;

/// <summary>
/// Tokens for public share links. A link is the whole credential, so it is
/// sized like one: 32 random bytes, URL-safe.
/// </summary>
public static class ShareTokens
{
    private const int TokenBytes = 32;

    /// <summary>Longer than any token this issues; anything else cannot be a real link.</summary>
    public const int MaxLength = 100;

    public static string Generate() =>
        WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(TokenBytes));

    public static bool IsWellFormed(string? token) =>
        !string.IsNullOrWhiteSpace(token) && token.Length <= MaxLength;
}
