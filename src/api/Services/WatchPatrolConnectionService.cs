using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using WatchTracker.Api.Data;
using WatchTracker.Api.DTOs;
using WatchTracker.Api.Models;

namespace WatchTracker.Api.Services;

public class WatchPatrolConnectionService(
    AppDbContext db,
    IHttpClientFactory clients,
    IDataProtectionProvider protection,
    ILogger<WatchPatrolConnectionService> logger) : IWatchPatrolConnectionService
{
    public const string Origin = "https://www.watchpatrol.net";
    // OAuth discovery advertises this canonical resource for the api.watchpatrol.net alias.
    public const string Resource = Origin + "/mcp/";
    private static readonly SemaphoreSlim CredentialGate = new(1, 1);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private IDataProtector Protector(int userId) =>
        protection.CreateProtector("WatchTracker.WatchPatrol", userId.ToString());

    public async Task<WatchPatrolStatusDto> GetStatusAsync(int userId, CancellationToken ct = default) =>
        new(await db.WatchPatrolConnections.AnyAsync(
            c => c.UserId == userId && c.CredentialsProtected != null, ct));

    public async Task<string> ConnectAsync(int userId, string redirectUri, CancellationToken ct = default)
    {
        await CredentialGate.WaitAsync(ct);
        try
        {
            using var client = clients.CreateClient("WatchPatrol");
            using var registration = await client.PostAsJsonAsync(Origin + "/oauth/register/", new
            {
                client_name = "WatchTracker Collection Advisor",
                redirect_uris = new[] { redirectUri },
                grant_types = new[] { "authorization_code", "refresh_token" },
                response_types = new[] { "code" },
                token_endpoint_auth_method = "none",
                scope = "mcp"
            }, ct);
            EnsureSuccess(registration);
            using var document = await ReadJsonAsync(registration, ct);
            var clientId = RequiredString(document.RootElement, "client_id");
            var state = RandomToken();
            var verifier = RandomToken();
            var connection = await db.WatchPatrolConnections
                .SingleOrDefaultAsync(c => c.UserId == userId, ct);
            if (connection is null)
            {
                connection = new WatchPatrolConnection { UserId = userId };
                db.WatchPatrolConnections.Add(connection);
            }
            var pending = new PendingAuthorization(clientId, redirectUri, verifier);
            connection.PendingStateHash = Hash(state);
            connection.PendingAuthorizationProtected = Protector(userId).Protect(
                JsonSerializer.Serialize(pending, JsonOptions));
            connection.PendingExpiresAt = DateTime.UtcNow.AddMinutes(10);
            await db.SaveChangesAsync(ct);

            return QueryHelpers.AddQueryString(Origin + "/oauth/authorize/",
                new Dictionary<string, string?>
                {
                    ["client_id"] = clientId,
                    ["redirect_uri"] = redirectUri,
                    ["response_type"] = "code",
                    ["scope"] = "mcp",
                    ["state"] = state,
                    ["code_challenge"] = WebEncoders.Base64UrlEncode(
                        SHA256.HashData(Encoding.ASCII.GetBytes(verifier))),
                    ["code_challenge_method"] = "S256",
                    ["resource"] = Resource
                });
        }
        finally
        {
            CredentialGate.Release();
        }
    }

    public async Task CompleteAsync(int userId, string code, string state, CancellationToken ct = default)
    {
        await CredentialGate.WaitAsync(ct);
        try
        {
            var stateHash = Hash(state);
            var now = DateTime.UtcNow;
            var connection = await db.WatchPatrolConnections.AsNoTracking()
                .SingleOrDefaultAsync(c => c.UserId == userId
                    && c.PendingStateHash == stateHash && c.PendingExpiresAt > now, ct);
            if (connection?.PendingAuthorizationProtected is null)
                throw new InvalidOperationException("The WatchPatrol connection request is invalid or expired. Connect again.");

            var pending = Unprotect<PendingAuthorization>(userId, connection.PendingAuthorizationProtected);
            // Consume before the network exchange; a replay cannot exchange the same authorization.
            var consumed = await db.WatchPatrolConnections
                .Where(c => c.UserId == userId && c.PendingStateHash == stateHash
                    && c.PendingExpiresAt > now)
                .ExecuteUpdateAsync(update => update
                    .SetProperty(c => c.PendingStateHash, (string?)null)
                    .SetProperty(c => c.PendingAuthorizationProtected, (string?)null)
                    .SetProperty(c => c.PendingExpiresAt, (DateTime?)null), ct);
            if (consumed != 1)
                throw new InvalidOperationException("The WatchPatrol connection request was already used. Connect again.");

            var credentials = await ExchangeAsync(pending.ClientId, new()
            {
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["redirect_uri"] = pending.RedirectUri,
                ["code_verifier"] = pending.Verifier
            }, null, ct);
            await SaveCredentialsAsync(userId, credentials, ct);
        }
        finally
        {
            CredentialGate.Release();
        }
    }

    public async Task<string> GetAccessTokenAsync(int userId, CancellationToken ct = default)
    {
        await CredentialGate.WaitAsync(ct);
        try
        {
            var value = await db.WatchPatrolConnections.AsNoTracking()
                .Where(c => c.UserId == userId)
                .Select(c => c.CredentialsProtected)
                .SingleOrDefaultAsync(ct);
            if (value is null)
                throw new InvalidOperationException("Connect your WatchPatrol account on the Advisor page first.");
            var credentials = Unprotect<Credentials>(userId, value);
            if (credentials.ExpiresAt > DateTime.UtcNow.AddMinutes(1))
                return credentials.AccessToken;
            if (credentials.RefreshToken is null)
                throw new InvalidOperationException("Your WatchPatrol authorization expired. Disconnect and connect again.");
            var refreshed = await ExchangeAsync(credentials.ClientId, new()
            {
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = credentials.RefreshToken
            }, credentials.RefreshToken, ct);
            await SaveCredentialsAsync(userId, refreshed, ct);
            return refreshed.AccessToken;
        }
        finally
        {
            CredentialGate.Release();
        }
    }

    public async Task DisconnectAsync(int userId, CancellationToken ct = default)
    {
        await CredentialGate.WaitAsync(ct);
        try
        {
            // Local removal is unconditional, including when the provider is unreachable.
            await db.WatchPatrolConnections.Where(c => c.UserId == userId).ExecuteDeleteAsync(ct);
        }
        finally
        {
            CredentialGate.Release();
        }
    }

    private async Task<Credentials> ExchangeAsync(
        string clientId,
        Dictionary<string, string> values,
        string? previousRefreshToken,
        CancellationToken ct)
    {
        values["client_id"] = clientId;
        values["resource"] = Resource;
        using var client = clients.CreateClient("WatchPatrol");
        using var response = await client.PostAsync(
            Origin + "/oauth/token/", new FormUrlEncodedContent(values), ct);
        EnsureSuccess(response);
        using var document = await ReadJsonAsync(response, ct);
        var root = document.RootElement;
        if (!RequiredString(root, "token_type").Equals("Bearer", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("WatchPatrol returned an unsupported token type.");
        if (!root.TryGetProperty("expires_in", out var expiry) || expiry.ValueKind != JsonValueKind.Number
            || !expiry.TryGetInt32(out var seconds) || seconds <= 0 || seconds > 31_536_000)
            throw new InvalidOperationException("WatchPatrol returned an invalid token expiration.");
        var refresh = root.TryGetProperty("refresh_token", out var token)
            && token.ValueKind == JsonValueKind.String
            ? token.GetString() : previousRefreshToken;
        return new Credentials(clientId, RequiredString(root, "access_token"),
            string.IsNullOrWhiteSpace(refresh) ? null : refresh, DateTime.UtcNow.AddSeconds(seconds));
    }

    private async Task SaveCredentialsAsync(int userId, Credentials credentials, CancellationToken ct)
    {
        var protectedValue = Protector(userId).Protect(JsonSerializer.Serialize(credentials, JsonOptions));
        var updated = await db.WatchPatrolConnections.Where(c => c.UserId == userId)
            .ExecuteUpdateAsync(update => update
                .SetProperty(c => c.CredentialsProtected, protectedValue), ct);
        if (updated != 1)
            throw new InvalidOperationException("The WatchPatrol connection was removed. Connect again.");
    }

    private T Unprotect<T>(int userId, string value) where T : class
    {
        try
        {
            return JsonSerializer.Deserialize<T>(Protector(userId).Unprotect(value), JsonOptions)
                ?? throw new JsonException();
        }
        catch (Exception ex) when (ex is CryptographicException or JsonException)
        {
            logger.LogWarning("Stored WatchPatrol authorization could not be read.");
            throw new InvalidOperationException(
                "Your WatchPatrol authorization could not be read. Disconnect and connect again.");
        }
    }

    private void EnsureSuccess(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode) return;
        logger.LogWarning("WatchPatrol OAuth returned HTTP {Status}.", (int)response.StatusCode);
        if (response.StatusCode == HttpStatusCode.TooManyRequests)
            throw new InvalidOperationException("WatchPatrol's rate limit was reached. Wait before trying again.");
        // Never expose response bodies: OAuth errors may contain credentials.
        throw new InvalidOperationException("WatchPatrol could not authorize this connection. Connect again.");
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            if (document.RootElement.ValueKind == JsonValueKind.Object)
                return document;
            document.Dispose();
            throw new JsonException();
        }
        catch (JsonException)
        {
            throw new InvalidOperationException("WatchPatrol returned an unreadable authorization response.");
        }
    }

    private static string RequiredString(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            && !string.IsNullOrWhiteSpace(value.GetString())
            ? value.GetString()!
            : throw new InvalidOperationException("WatchPatrol returned an incomplete authorization response.");

    private static string RandomToken() => WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    private sealed record PendingAuthorization(string ClientId, string RedirectUri, string Verifier);
    private sealed record Credentials(string ClientId, string AccessToken, string? RefreshToken, DateTime ExpiresAt);
}
