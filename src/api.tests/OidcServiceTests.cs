using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using WatchTracker.Api.DTOs;
using WatchTracker.Api.Models;
using WatchTracker.Api.Services;

namespace WatchTracker.Api.Tests;

public class OidcServiceTests
{
    [Fact]
    public async Task Login_url_persists_state_and_uses_s256_pkce_with_safe_return_url()
    {
        await using var database = await TestDatabase.CreateAsync();
        database.Context.OidcProviderSettings.Add(new OidcProviderSetting
        {
            Provider = OidcProvider.Entra,
            Enabled = true,
            DisplayName = "Entra",
            Authority = "https://identity.example.test",
            ClientId = "client-id",
            Scopes = "profile email"
        });
        await database.Context.SaveChangesAsync();
        var protector = new EphemeralDataProtectionProvider();
        var service = new OidcService(database.Context, new DiscoveryClientFactory(), protector, new StubAuthService());
        var request = new DefaultHttpContext().Request;
        request.Scheme = "https";
        request.Host = new HostString("tracker.example.test");

        var url = await service.BuildLoginUrlAsync(OidcProvider.Entra, request, "https://attacker.example.test");

        Assert.NotNull(url);
        var query = QueryHelpers.ParseQuery(new Uri(url!).Query);
        Assert.Equal("code", query["response_type"]);
        Assert.Equal("S256", query["code_challenge_method"]);
        Assert.Equal("openid profile email", query["scope"]);
        Assert.Equal("https://tracker.example.test/api/auth/oidc/Entra/complete", query["redirect_uri"]);
        Assert.False(string.IsNullOrWhiteSpace(query["state"]));
        Assert.False(string.IsNullOrWhiteSpace(query["nonce"]));

        var stored = Assert.Single(database.Context.OidcStates);
        Assert.Equal("/", stored.ReturnUrl);
        Assert.Equal(Hash(query["state"]!), stored.StateHash);
        Assert.Equal(Hash(query["nonce"]!), stored.NonceHash);
        var verifier = protector.CreateProtector("WatchTracker.Oidc.State").Unprotect(stored.CodeVerifierProtected);
        Assert.Equal(WebEncoders.Base64UrlEncode(SHA256.HashData(Encoding.ASCII.GetBytes(verifier))), query["code_challenge"]);
    }

    [Fact]
    public async Task Callback_rejects_unknown_state_without_creating_login_ticket()
    {
        await using var database = await TestDatabase.CreateAsync();
        var service = new OidcService(database.Context, new DiscoveryClientFactory(), new EphemeralDataProtectionProvider(), new StubAuthService());
        var request = new DefaultHttpContext().Request;

        var redirect = await service.CompleteLoginAsync(OidcProvider.Entra, request, "code", "unknown-state", null);

        Assert.Contains("oidc_invalid_state", redirect);
        Assert.Empty(database.Context.OidcLoginTickets);
    }

    private static string Hash(string value) => Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private sealed class DiscoveryClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(new DiscoveryHandler());
    }

    private sealed class DiscoveryHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("""{"issuer":"https://identity.example.test","authorization_endpoint":"https://identity.example.test/authorize","token_endpoint":"https://identity.example.test/token","jwks_uri":"https://identity.example.test/keys"}""")
            });
    }

    private sealed class StubAuthService : IAuthService
    {
        public Task<AuthResponseDto?> RegisterAsync(RegisterDto dto) => Task.FromResult<AuthResponseDto?>(null);
        public Task<AuthResponseDto?> LoginAsync(LoginDto dto) => Task.FromResult<AuthResponseDto?>(null);
        public Task<bool> ChangePasswordAsync(int userId, ChangePasswordDto dto) => Task.FromResult(false);
        public Task<AuthResponseDto?> GetProfileAsync(int userId) => Task.FromResult<AuthResponseDto?>(null);
        public Task SetProfileImageAsync(int userId, string fileName) => Task.CompletedTask;
        public Task<string?> DeleteProfileImageAsync(int userId) => Task.FromResult<string?>(null);
        public Task<bool> UpdateUsernameAsync(int userId, string username) => Task.FromResult(false);
        public Task<List<string>?> UpdateStorageLocationsAsync(int userId, IEnumerable<string> storageLocations) => Task.FromResult<List<string>?>(null);
        public Task<AuthResponseDto?> RefreshAsync(string refreshToken) => Task.FromResult<AuthResponseDto?>(null);
        public Task RevokeRefreshTokenAsync(string refreshToken) => Task.CompletedTask;
        public Task<AuthResponseDto?> IssueTokensForUserAsync(int userId) => Task.FromResult<AuthResponseDto?>(null);
    }
}
