using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using WatchTracker.Api.Services;

namespace WatchTracker.Api.Tests;

public class WatchPatrolConnectionServiceTests
{
    [Fact]
    public async Task OAuth_uses_PKCE_and_encrypted_user_scoped_credentials_and_rejects_replay()
    {
        await using var database = await TestDatabase.CreateAsync();
        var owner = TestDatabase.User("owner");
        var other = TestDatabase.User("other");
        database.Context.Users.AddRange(owner, other);
        await database.Context.SaveChangesAsync();
        var http = new OAuthHandler();
        var service = Create(database, http);

        var url = new Uri(await service.ConnectAsync(owner.Id, "https://tracker.test/advisor"));
        var query = QueryHelpers.ParseQuery(url.Query);
        var state = query["state"].ToString();
        Assert.Equal("https://www.watchpatrol.net", url.GetLeftPart(UriPartial.Authority));
        Assert.Equal("mcp", query["scope"].ToString());
        Assert.Equal("S256", query["code_challenge_method"].ToString());
        Assert.Equal(WatchPatrolConnectionService.Resource, query["resource"].ToString());
        Assert.Contains("\"token_endpoint_auth_method\":\"none\"", http.Requests[0].Body);
        Assert.Contains("\"scope\":\"mcp\"", http.Requests[0].Body);
        Assert.DoesNotContain("client_secret", http.Requests[0].Body);
        Assert.False((await service.GetStatusAsync(owner.Id)).Connected);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CompleteAsync(other.Id, "authorization-code", state));
        Assert.Single(http.Requests);
        await service.CompleteAsync(owner.Id, "authorization-code", state);

        var tokenRequest = QueryHelpers.ParseQuery(http.Requests[1].Body);
        var challenge = WebEncoders.Base64UrlEncode(
            SHA256.HashData(Encoding.ASCII.GetBytes(tokenRequest["code_verifier"].ToString())));
        Assert.Equal(query["code_challenge"].ToString(), challenge);
        Assert.Equal("https://tracker.test/advisor", tokenRequest["redirect_uri"].ToString());
        Assert.Equal("authorization_code", tokenRequest["grant_type"].ToString());
        Assert.Equal("test-client", tokenRequest["client_id"].ToString());
        Assert.Equal("authorization-code", tokenRequest["code"].ToString());
        Assert.True((await service.GetStatusAsync(owner.Id)).Connected);
        Assert.False((await service.GetStatusAsync(other.Id)).Connected);
        Assert.Equal("test-access", await service.GetAccessTokenAsync(owner.Id));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetAccessTokenAsync(other.Id));
        var stored = await database.Context.WatchPatrolConnections.AsNoTracking().SingleAsync();
        Assert.NotNull(stored.CredentialsProtected);
        Assert.DoesNotContain("test-access", stored.CredentialsProtected);
        Assert.DoesNotContain("test-refresh", stored.CredentialsProtected);
        Assert.Null(stored.PendingAuthorizationProtected);
        Assert.Null(stored.PendingStateHash);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CompleteAsync(owner.Id, "authorization-code", state));
        Assert.Equal(2, http.Requests.Count);
        Assert.False(database.Context.Database.HasPendingModelChanges());
    }

    [Fact]
    public async Task Expired_or_disconnected_authorization_cannot_be_completed()
    {
        await using var database = await TestDatabase.CreateAsync();
        var user = TestDatabase.User("owner");
        database.Context.Users.Add(user);
        await database.Context.SaveChangesAsync();
        var http = new OAuthHandler();
        var service = Create(database, http);
        var state = State(await service.ConnectAsync(user.Id, "https://tracker.test/advisor"));
        await database.Context.WatchPatrolConnections.ExecuteUpdateAsync(update => update
            .SetProperty(c => c.PendingExpiresAt, DateTime.UtcNow.AddMinutes(-1)));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CompleteAsync(user.Id, "code", state));
        Assert.Single(http.Requests);
        await service.DisconnectAsync(user.Id);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CompleteAsync(user.Id, "code", state));
        Assert.False((await service.GetStatusAsync(user.Id)).Connected);
        Assert.Single(http.Requests);
    }

    [Fact]
    public async Task Expiring_token_is_refreshed_and_disconnect_removes_future_access()
    {
        await using var database = await TestDatabase.CreateAsync();
        var user = TestDatabase.User("owner");
        database.Context.Users.Add(user);
        await database.Context.SaveChangesAsync();
        var http = new OAuthHandler { InitialLifetime = 1 };
        var service = Create(database, http);
        var state = State(await service.ConnectAsync(user.Id, "https://tracker.test/advisor"));
        await service.CompleteAsync(user.Id, "code", state);

        Assert.Equal("refreshed-access", await service.GetAccessTokenAsync(user.Id));
        var refresh = QueryHelpers.ParseQuery(http.Requests[2].Body);
        Assert.Equal("refresh_token", refresh["grant_type"].ToString());
        Assert.Equal("test-refresh", refresh["refresh_token"].ToString());
        Assert.Equal(WatchPatrolConnectionService.Resource, refresh["resource"].ToString());
        Assert.Equal("refreshed-access", await service.GetAccessTokenAsync(user.Id));
        Assert.Equal(3, http.Requests.Count);
        await service.DisconnectAsync(user.Id);
        await service.DisconnectAsync(user.Id);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetAccessTokenAsync(user.Id));
    }

    [Fact]
    public async Task Failed_token_exchange_does_not_connect_or_allow_state_reuse()
    {
        await using var database = await TestDatabase.CreateAsync();
        var user = TestDatabase.User("owner");
        database.Context.Users.Add(user);
        await database.Context.SaveChangesAsync();
        var http = new OAuthHandler { TokenStatus = HttpStatusCode.BadRequest };
        var service = Create(database, http);
        var state = State(await service.ConnectAsync(user.Id, "https://tracker.test/advisor"));

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CompleteAsync(user.Id, "code", state));
        Assert.DoesNotContain("private-provider-body", error.Message);
        Assert.False((await service.GetStatusAsync(user.Id)).Connected);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CompleteAsync(user.Id, "code", state));
        Assert.Equal(2, http.Requests.Count);
    }

    [Fact]
    public async Task Credentials_cannot_be_copied_to_another_user()
    {
        await using var database = await TestDatabase.CreateAsync();
        var owner = TestDatabase.User("owner");
        var other = TestDatabase.User("other");
        database.Context.Users.AddRange(owner, other);
        await database.Context.SaveChangesAsync();
        var service = Create(database, new OAuthHandler());
        await service.CompleteAsync(owner.Id, "code",
            State(await service.ConnectAsync(owner.Id, "https://tracker.test/advisor")));
        var encrypted = await database.Context.WatchPatrolConnections.AsNoTracking()
            .Where(c => c.UserId == owner.Id).Select(c => c.CredentialsProtected).SingleAsync();
        database.Context.WatchPatrolConnections.Add(new()
        {
            UserId = other.Id,
            CredentialsProtected = encrypted
        });
        await database.Context.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetAccessTokenAsync(other.Id));
        Assert.Equal("test-access", await service.GetAccessTokenAsync(owner.Id));
    }

    private static string State(string url) =>
        QueryHelpers.ParseQuery(new Uri(url).Query)["state"].ToString();

    private static WatchPatrolConnectionService Create(TestDatabase database, OAuthHandler handler) =>
        new(database.Context, new Factory(handler), new EphemeralDataProtectionProvider(),
            NullLogger<WatchPatrolConnectionService>.Instance);

    private sealed class Factory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private sealed class OAuthHandler : HttpMessageHandler
    {
        public int InitialLifetime { get; init; } = 3600;
        public HttpStatusCode TokenStatus { get; init; } = HttpStatusCode.OK;
        public List<(string Path, string Body)> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken ct)
        {
            var body = await request.Content!.ReadAsStringAsync(ct);
            Requests.Add((request.RequestUri!.AbsolutePath, body));
            Assert.Equal("www.watchpatrol.net", request.RequestUri.Host);
            if (request.RequestUri.AbsolutePath == "/oauth/register/")
                return Json(new { client_id = "test-client" });
            Assert.Equal("/oauth/token/", request.RequestUri.AbsolutePath);
            if (TokenStatus != HttpStatusCode.OK)
                return new(TokenStatus) { Content = new StringContent("private-provider-body") };
            var refresh = QueryHelpers.ParseQuery(body)["grant_type"] == "refresh_token";
            return Json(new
            {
                token_type = "Bearer",
                access_token = refresh ? "refreshed-access" : "test-access",
                refresh_token = refresh ? "rotated-refresh" : "test-refresh",
                expires_in = refresh ? 3600 : InitialLifetime
            });
        }

        private static HttpResponseMessage Json(object value) =>
            new(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json")
            };
    }
}
