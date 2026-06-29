using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using WatchTracker.Api.Data;
using WatchTracker.Api.DTOs;
using WatchTracker.Api.Models;

namespace WatchTracker.Api.Services;

public class OidcService(
    AppDbContext context,
    IHttpClientFactory httpClientFactory,
    IDataProtectionProvider dataProtectionProvider,
    IAuthService authService) : IOidcService
{
    private static readonly JwtSecurityTokenHandler TokenHandler = new();
    private readonly IDataProtector secretProtector = dataProtectionProvider.CreateProtector("WatchTracker.Oidc.ClientSecret");
    private readonly IDataProtector stateProtector = dataProtectionProvider.CreateProtector("WatchTracker.Oidc.State");

    public async Task<List<OidcProviderSettingsDto>> GetAdminProvidersAsync(CancellationToken ct = default)
    {
        await EnsureDefaultProvidersAsync(ct);

        return await context.OidcProviderSettings
            .OrderBy(s => s.Provider)
            .Select(s => new OidcProviderSettingsDto
            {
                Provider = s.Provider,
                Enabled = s.Enabled,
                DisplayName = s.DisplayName,
                Authority = s.Authority,
                ClientId = s.ClientId,
                Scopes = s.Scopes,
                HasClientSecret = s.ClientSecretProtected != null,
                UpdatedAt = s.UpdatedAt
            })
            .ToListAsync(ct);
    }

    public async Task<OidcProviderSettingsDto?> UpdateProviderAsync(
        OidcProvider provider,
        UpdateOidcProviderSettingsDto dto,
        CancellationToken ct = default)
    {
        var setting = await GetOrCreateProviderAsync(provider, ct);
        setting.Enabled = dto.Enabled;
        setting.DisplayName = dto.DisplayName;
        setting.Authority = TrimTrailingSlash(dto.Authority);
        setting.ClientId = dto.ClientId;
        setting.Scopes = NormalizeScopes(dto.Scopes);
        setting.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(ct);

        return new OidcProviderSettingsDto
        {
            Provider = setting.Provider,
            Enabled = setting.Enabled,
            DisplayName = setting.DisplayName,
            Authority = setting.Authority,
            ClientId = setting.ClientId,
            Scopes = setting.Scopes,
            HasClientSecret = setting.ClientSecretProtected != null,
            UpdatedAt = setting.UpdatedAt
        };
    }

    public async Task<bool> SetClientSecretAsync(
        OidcProvider provider,
        string clientSecret,
        CancellationToken ct = default)
    {
        var setting = await GetOrCreateProviderAsync(provider, ct);
        setting.ClientSecretProtected = secretProtector.Protect(clientSecret);
        setting.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<OidcProviderTestResultDto> TestProviderAsync(OidcProvider provider, CancellationToken ct = default)
    {
        var setting = await context.OidcProviderSettings.FirstOrDefaultAsync(s => s.Provider == provider, ct);
        if (setting is null)
            return new OidcProviderTestResultDto { Success = false, Message = "Provider is not configured." };

        if (!HasRequiredSettings(setting))
            return new OidcProviderTestResultDto { Success = false, Message = "Authority and client ID are required." };

        try
        {
            var configuration = await GetConfigurationAsync(setting, ct);
            if (string.IsNullOrWhiteSpace(configuration.AuthorizationEndpoint) ||
                string.IsNullOrWhiteSpace(configuration.TokenEndpoint))
            {
                return new OidcProviderTestResultDto
                {
                    Success = false,
                    Message = "Discovery metadata is missing authorization or token endpoints."
                };
            }

            return new OidcProviderTestResultDto
            {
                Success = true,
                Message = $"Discovery succeeded for issuer {configuration.Issuer}."
            };
        }
        catch (Exception ex)
        {
            return new OidcProviderTestResultDto { Success = false, Message = ex.Message };
        }
    }

    public async Task<List<OidcProviderPublicDto>> GetEnabledProvidersAsync(CancellationToken ct = default)
    {
        return await context.OidcProviderSettings
            .Where(s => s.Enabled && s.Authority != "" && s.ClientId != "")
            .OrderBy(s => s.Provider)
            .Select(s => new OidcProviderPublicDto
            {
                Provider = s.Provider,
                DisplayName = s.DisplayName
            })
            .ToListAsync(ct);
    }

    public async Task<string?> BuildLoginUrlAsync(
        OidcProvider provider,
        HttpRequest request,
        string? returnUrl,
        int? linkUserId = null,
        CancellationToken ct = default)
    {
        var setting = await context.OidcProviderSettings.FirstOrDefaultAsync(s => s.Provider == provider, ct);
        if (setting is null || !setting.Enabled || !HasRequiredSettings(setting))
            return null;

        var configuration = await GetConfigurationAsync(setting, ct);
        var state = CreateRandomToken(32);
        var codeVerifier = CreateRandomToken(64);
        var nonce = CreateRandomToken(32);
        var stateHash = HashToken(state);

        context.OidcStates.Add(new OidcState
        {
            StateHash = stateHash,
            Provider = provider,
            CodeVerifierProtected = stateProtector.Protect(codeVerifier),
            NonceHash = HashToken(nonce),
            ReturnUrl = NormalizeReturnUrl(returnUrl),
            LinkUserId = linkUserId,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10)
        });
        await context.SaveChangesAsync(ct);

        var redirectUri = BuildCallbackUri(request, provider);
        var query = new Dictionary<string, string?>
        {
            ["client_id"] = setting.ClientId,
            ["redirect_uri"] = redirectUri,
            ["response_type"] = "code",
            ["scope"] = NormalizeScopes(setting.Scopes),
            ["state"] = state,
            ["nonce"] = nonce,
            ["code_challenge"] = CreateCodeChallenge(codeVerifier),
            ["code_challenge_method"] = "S256"
        };

        return QueryHelpers.AddQueryString(configuration.AuthorizationEndpoint, query);
    }

    public async Task<string> CompleteLoginAsync(
        OidcProvider provider,
        HttpRequest request,
        string? code,
        string? state,
        string? error,
        CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(error))
            return BuildErrorRedirect("oidc_provider_error");

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(state))
            return BuildErrorRedirect("oidc_callback_missing_code");

        var stateHash = HashToken(state);
        var storedState = await context.OidcStates
            .FirstOrDefaultAsync(s => s.StateHash == stateHash && s.Provider == provider, ct);

        if (storedState is null || !storedState.IsActive)
            return BuildErrorRedirect("oidc_invalid_state");

        storedState.ConsumedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(ct);

        var setting = await context.OidcProviderSettings.FirstOrDefaultAsync(s => s.Provider == provider, ct);
        if (setting is null || !setting.Enabled || !HasRequiredSettings(setting))
            return BuildErrorRedirect("oidc_provider_disabled");

        ClaimsPrincipal principal;
        try
        {
            var codeVerifier = stateProtector.Unprotect(storedState.CodeVerifierProtected);
            var configuration = await GetConfigurationAsync(setting, ct);
            var tokenResponse = await ExchangeAuthorizationCodeAsync(
                setting,
                configuration,
                code,
                codeVerifier,
                BuildCallbackUri(request, provider),
                ct);

            if (string.IsNullOrWhiteSpace(tokenResponse.IdToken))
                return BuildErrorRedirect("oidc_missing_id_token");

            principal = ValidateIdToken(tokenResponse.IdToken, setting, configuration, storedState.NonceHash);
        }
        catch (HttpRequestException)
        {
            return BuildErrorRedirect("oidc_token_exchange_failed");
        }
        catch (InvalidOperationException)
        {
            return BuildErrorRedirect("oidc_token_exchange_failed");
        }
        catch (JsonException)
        {
            return BuildErrorRedirect("oidc_token_exchange_failed");
        }
        catch (SecurityTokenException)
        {
            return BuildErrorRedirect("oidc_token_validation_failed");
        }
        catch (CryptographicException)
        {
            return BuildErrorRedirect("oidc_invalid_state");
        }

        var signInUserId = await ResolveUserAsync(provider, principal, storedState.LinkUserId, ct);
        if (signInUserId is null)
            return BuildErrorRedirect("oidc_account_not_linked");

        var handoffCode = CreateRandomToken(48);
        context.OidcLoginTickets.Add(new OidcLoginTicket
        {
            CodeHash = HashToken(handoffCode),
            UserId = signInUserId.Value,
            ReturnUrl = storedState.ReturnUrl,
            ExpiresAt = DateTime.UtcNow.AddMinutes(2)
        });
        await context.SaveChangesAsync(ct);

        var callbackUrl = QueryHelpers.AddQueryString("/oidc/callback", new Dictionary<string, string?>
        {
            ["code"] = handoffCode,
            ["returnUrl"] = storedState.ReturnUrl ?? "/"
        });

        return callbackUrl;
    }

    public async Task<AuthResponseDto?> ExchangeCodeAsync(string code, CancellationToken ct = default)
    {
        var codeHash = HashToken(code);
        var now = DateTime.UtcNow;
        var updated = await context.OidcLoginTickets
            .Where(t => t.CodeHash == codeHash && t.ConsumedAt == null && t.ExpiresAt > now)
            .ExecuteUpdateAsync(updates => updates.SetProperty(t => t.ConsumedAt, now), ct);

        if (updated != 1)
            return null;

        var userId = await context.OidcLoginTickets
            .Where(t => t.CodeHash == codeHash && t.ConsumedAt == now)
            .Select(t => t.UserId)
            .SingleAsync(ct);

        return await authService.IssueTokensForUserAsync(userId);
    }

    public async Task<List<LinkedOidcProviderDto>> GetLinkedProvidersAsync(int userId, CancellationToken ct = default)
    {
        var displayNames = await context.OidcProviderSettings
            .ToDictionaryAsync(s => s.Provider, s => s.DisplayName, ct);

        var links = await context.ExternalLogins
            .Where(l => l.UserId == userId)
            .OrderBy(l => l.Provider)
            .ToListAsync(ct);

        return links.Select(l => new LinkedOidcProviderDto
        {
            Provider = l.Provider,
            DisplayName = displayNames.GetValueOrDefault(l.Provider, GetDefaultDisplayName(l.Provider)),
            Email = l.Email,
            LinkedAt = l.CreatedAt,
            LastUsedAt = l.LastUsedAt
        }).ToList();
    }

    public async Task<bool> UnlinkProviderAsync(int userId, OidcProvider provider, CancellationToken ct = default)
    {
        var link = await context.ExternalLogins
            .FirstOrDefaultAsync(l => l.UserId == userId && l.Provider == provider, ct);
        if (link is null) return false;

        context.ExternalLogins.Remove(link);
        await context.SaveChangesAsync(ct);
        return true;
    }

    private async Task<int?> ResolveUserAsync(
        OidcProvider provider,
        ClaimsPrincipal principal,
        int? linkUserId,
        CancellationToken ct)
    {
        var subject = GetRequiredClaim(principal, JwtRegisteredClaimNames.Sub, ClaimTypes.NameIdentifier);
        var issuer = GetRequiredClaim(principal, JwtRegisteredClaimNames.Iss, "iss");
        var email = GetEmail(principal);
        if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(email))
            return null;

        var existingLink = await context.ExternalLogins
            .Include(l => l.User)
            .FirstOrDefaultAsync(l =>
                l.Provider == provider &&
                l.Issuer == issuer &&
                l.ProviderSubject == subject,
                ct);

        if (existingLink is not null)
        {
            if (linkUserId is not null && existingLink.UserId != linkUserId)
                return null;

            existingLink.LastUsedAt = DateTime.UtcNow;
            await context.SaveChangesAsync(ct);
            return existingLink.UserId;
        }

        if (linkUserId is null && !IsEmailVerified(principal))
            return null;

        var normalizedEmail = email.ToLowerInvariant();
        var user = linkUserId is null
            ? await context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail, ct)
            : await context.Users.FirstOrDefaultAsync(u => u.Id == linkUserId && u.Email.ToLower() == normalizedEmail, ct);

        if (user is null)
            return null;

        var existingProviderForUser = await context.ExternalLogins
            .AnyAsync(l => l.UserId == user.Id && l.Provider == provider, ct);
        if (existingProviderForUser)
            return null;

        context.ExternalLogins.Add(new ExternalLogin
        {
            UserId = user.Id,
            Provider = provider,
            Issuer = issuer,
            ProviderSubject = subject,
            Email = email,
            CreatedAt = DateTime.UtcNow,
            LastUsedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync(ct);
        return user.Id;
    }

    private async Task<OidcProviderSetting> GetOrCreateProviderAsync(OidcProvider provider, CancellationToken ct)
    {
        var setting = await context.OidcProviderSettings.FirstOrDefaultAsync(s => s.Provider == provider, ct);
        if (setting is not null) return setting;

        setting = new OidcProviderSetting
        {
            Provider = provider,
            Enabled = false,
            DisplayName = GetDefaultDisplayName(provider),
            Authority = "",
            ClientId = "",
            Scopes = "openid profile email",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.OidcProviderSettings.Add(setting);
        await context.SaveChangesAsync(ct);
        return setting;
    }

    private async Task EnsureDefaultProvidersAsync(CancellationToken ct)
    {
        foreach (var provider in Enum.GetValues<OidcProvider>())
        {
            _ = await GetOrCreateProviderAsync(provider, ct);
        }
    }

    private async Task<OpenIdConnectConfiguration> GetConfigurationAsync(
        OidcProviderSetting setting,
        CancellationToken ct)
    {
        var metadataAddress = $"{TrimTrailingSlash(setting.Authority)}/.well-known/openid-configuration";
        var documentRetriever = new HttpDocumentRetriever(httpClientFactory.CreateClient())
        {
            RequireHttps = setting.Authority.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
        };
        var manager = new ConfigurationManager<OpenIdConnectConfiguration>(
            metadataAddress,
            new OpenIdConnectConfigurationRetriever(),
            documentRetriever);

        return await manager.GetConfigurationAsync(ct);
    }

    private async Task<TokenResponse> ExchangeAuthorizationCodeAsync(
        OidcProviderSetting setting,
        OpenIdConnectConfiguration configuration,
        string code,
        string codeVerifier,
        string redirectUri,
        CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, configuration.TokenEndpoint);
        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = redirectUri,
            ["client_id"] = setting.ClientId,
            ["code_verifier"] = codeVerifier
        };

        if (!string.IsNullOrWhiteSpace(setting.ClientSecretProtected))
            form["client_secret"] = secretProtector.Unprotect(setting.ClientSecretProtected);

        request.Content = new FormUrlEncodedContent(form);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

        using var response = await httpClientFactory.CreateClient().SendAsync(request, ct);
        var content = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException("OIDC token exchange failed.");

        using var json = JsonDocument.Parse(content);
        return new TokenResponse(json.RootElement.GetProperty("id_token").GetString());
    }

    private ClaimsPrincipal ValidateIdToken(
        string idToken,
        OidcProviderSetting setting,
        OpenIdConnectConfiguration configuration,
        string expectedNonceHash)
    {
        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = configuration.Issuer,
            ValidateAudience = true,
            ValidAudience = setting.ClientId,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKeys = configuration.SigningKeys,
            ClockSkew = TimeSpan.FromMinutes(2)
        };

        var principal = TokenHandler.ValidateToken(idToken, parameters, out _);
        var nonce = principal.FindFirstValue(JwtRegisteredClaimNames.Nonce) ?? principal.FindFirstValue("nonce");
        if (string.IsNullOrWhiteSpace(nonce) || HashToken(nonce) != expectedNonceHash)
            throw new SecurityTokenValidationException("OIDC nonce validation failed.");

        return principal;
    }

    private static string? GetRequiredClaim(ClaimsPrincipal principal, params string[] claimTypes)
    {
        foreach (var claimType in claimTypes)
        {
            var value = principal.FindFirstValue(claimType);
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }

        return null;
    }

    private static string? GetEmail(ClaimsPrincipal principal) =>
        GetRequiredClaim(principal, JwtRegisteredClaimNames.Email, ClaimTypes.Email, "preferred_username", "upn");

    private static bool IsEmailVerified(ClaimsPrincipal principal)
    {
        var claim = principal.FindFirst("email_verified");
        return claim is not null && bool.TryParse(claim.Value, out var verified) && verified;
    }

    private static string BuildCallbackUri(HttpRequest request, OidcProvider provider) =>
        $"{request.Scheme}://{request.Host}/api/auth/oidc/{provider}/complete";

    private static string NormalizeReturnUrl(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl)) return "/";
        return returnUrl.StartsWith('/') && !returnUrl.StartsWith("//") && !returnUrl.Contains("://")
            ? returnUrl
            : "/";
    }

    private static string BuildErrorRedirect(string error) =>
        QueryHelpers.AddQueryString("/login", "oidcError", error);

    private static string CreateRandomToken(int byteLength)
    {
        var bytes = new byte[byteLength];
        RandomNumberGenerator.Fill(bytes);
        return WebEncoders.Base64UrlEncode(bytes);
    }

    private static string CreateCodeChallenge(string codeVerifier)
    {
        var bytes = SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier));
        return WebEncoders.Base64UrlEncode(bytes);
    }

    private static string HashToken(string token) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private static string NormalizeScopes(string scopes)
    {
        var values = scopes
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (!values.Contains("openid", StringComparer.OrdinalIgnoreCase))
            values.Insert(0, "openid");

        return string.Join(' ', values);
    }

    private static string TrimTrailingSlash(string value) => value.Trim().TrimEnd('/');

    private static bool HasRequiredSettings(OidcProviderSetting setting) =>
        !string.IsNullOrWhiteSpace(setting.Authority) &&
        !string.IsNullOrWhiteSpace(setting.ClientId);

    private static string GetDefaultDisplayName(OidcProvider provider) => provider switch
    {
        OidcProvider.Entra => "Microsoft Entra ID",
        OidcProvider.PocketId => "Pocket ID",
        _ => provider.ToString()
    };

    private sealed record TokenResponse(string? IdToken);
}
