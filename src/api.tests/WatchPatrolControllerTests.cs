using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using WatchTracker.Api.Controllers;
using WatchTracker.Api.DTOs;
using WatchTracker.Api.Services;

namespace WatchTracker.Api.Tests;

public class WatchPatrolControllerTests
{
    [Theory]
    [InlineData("https://tracker.test/advisor", true)]
    [InlineData("http://localhost:5173/advisor", true)]
    [InlineData("https://untrusted.test/advisor", false)]
    [InlineData("https://tracker.test/other", false)]
    [InlineData("https://tracker.test/advisor?redirect=https://untrusted.test", false)]
    [InlineData("https://tracker.test/advisor#fragment", false)]
    [InlineData("https://someone@tracker.test/advisor", false)]
    [InlineData("http://tracker.test/advisor", false)]
    [InlineData("javascript:alert(1)", false)]
    public async Task Only_the_apps_advisor_page_can_receive_authorization(string redirect, bool expected)
    {
        var service = new StubConnections();
        var controller = Create(service);
        var result = await controller.Connect(new() { RedirectUri = redirect }, default);
        Assert.Equal(expected, result.Result is OkObjectResult);
        Assert.Equal<int?>(expected ? 7 : null, service.LastUserId);
    }

    [Fact]
    public async Task Completion_status_and_disconnect_are_scoped_to_authenticated_user()
    {
        var service = new StubConnections();
        var controller = Create(service);
        Assert.IsType<OkObjectResult>((await controller.GetStatus(default)).Result);
        Assert.Equal(7, service.LastUserId);
        Assert.IsType<OkObjectResult>((await controller.Complete(
            new() { Code = "code", State = "state" }, default)).Result);
        Assert.Equal(7, service.LastUserId);
        Assert.IsType<NoContentResult>(await controller.Disconnect(default));
        Assert.Equal(7, service.LastUserId);
    }

    [Fact]
    public async Task Network_failure_returns_safe_user_facing_error()
    {
        var service = new StubConnections { Failure = new HttpRequestException("private-details") };
        var controller = Create(service);
        var result = await controller.Connect(new() { RedirectUri = "https://tracker.test/advisor" }, default);
        var error = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.DoesNotContain("private-details", System.Text.Json.JsonSerializer.Serialize(error.Value));
    }

    private static WatchPatrolController Create(StubConnections service)
    {
        var controller = new WatchPatrolController(service, new ConfigurationBuilder().Build(),
            NullLogger<WatchPatrolController>.Instance)
        {
            ControllerContext = new()
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        [new Claim(ClaimTypes.NameIdentifier, "7")], "Test"))
                }
            }
        };
        controller.Request.Scheme = "https";
        controller.Request.Host = new HostString("tracker.test");
        return controller;
    }

    private sealed class StubConnections : IWatchPatrolConnectionService
    {
        public int? LastUserId { get; private set; }
        public Exception? Failure { get; init; }
        public Task<WatchPatrolStatusDto> GetStatusAsync(int userId, CancellationToken ct = default)
        {
            LastUserId = userId;
            return Task.FromResult(new WatchPatrolStatusDto(true));
        }
        public Task<string> ConnectAsync(int userId, string redirectUri, CancellationToken ct = default)
        {
            LastUserId = userId;
            return Failure is null ? Task.FromResult("https://www.watchpatrol.net/oauth/authorize/")
                : Task.FromException<string>(Failure);
        }
        public Task CompleteAsync(int userId, string code, string state, CancellationToken ct = default)
        {
            LastUserId = userId;
            return Task.CompletedTask;
        }
        public Task DisconnectAsync(int userId, CancellationToken ct = default)
        {
            LastUserId = userId;
            return Task.CompletedTask;
        }
        public Task<string> GetAccessTokenAsync(int userId, CancellationToken ct = default) =>
            throw new NotSupportedException();
    }
}
