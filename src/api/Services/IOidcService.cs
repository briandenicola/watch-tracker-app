using WatchTracker.Api.DTOs;
using WatchTracker.Api.Models;

namespace WatchTracker.Api.Services;

public interface IOidcService
{
    Task<List<OidcProviderSettingsDto>> GetAdminProvidersAsync(CancellationToken ct = default);
    Task<OidcProviderSettingsDto?> UpdateProviderAsync(
        OidcProvider provider,
        UpdateOidcProviderSettingsDto dto,
        CancellationToken ct = default);
    Task<bool> SetClientSecretAsync(
        OidcProvider provider,
        string clientSecret,
        CancellationToken ct = default);
    Task<OidcProviderTestResultDto> TestProviderAsync(OidcProvider provider, CancellationToken ct = default);
    Task<List<OidcProviderPublicDto>> GetEnabledProvidersAsync(CancellationToken ct = default);
    Task<string?> BuildLoginUrlAsync(
        OidcProvider provider,
        HttpRequest request,
        string? returnUrl,
        int? linkUserId = null,
        CancellationToken ct = default);
    Task<string> CompleteLoginAsync(
        OidcProvider provider,
        HttpRequest request,
        string? code,
        string? state,
        string? error,
        CancellationToken ct = default);
    Task<AuthResponseDto?> ExchangeCodeAsync(string code, CancellationToken ct = default);
    Task<List<LinkedOidcProviderDto>> GetLinkedProvidersAsync(int userId, CancellationToken ct = default);
    Task<bool> UnlinkProviderAsync(int userId, OidcProvider provider, CancellationToken ct = default);
}
