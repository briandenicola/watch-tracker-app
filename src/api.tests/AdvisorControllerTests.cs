using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using WatchTracker.Api.Controllers;
using WatchTracker.Api.DTOs;
using WatchTracker.Api.Services;

namespace WatchTracker.Api.Tests;

public class AdvisorControllerTests
{
    [Fact]
    public async Task GetSession_returns_not_found_when_service_cannot_find_owned_session()
    {
        var controller = CreateController(new StubAdvisorService());

        var result = await controller.GetSession(42, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task SendMessage_returns_bad_request_for_generation_failure()
    {
        var service = new StubAdvisorService
        {
            SendFailure = new InvalidOperationException("model failed")
        };
        var controller = CreateController(service);

        var result = await controller.SendMessage(
            42,
            new SendAdvisorMessageDto { Message = "Help me" },
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    private static AdvisorController CreateController(ICollectionAdvisorService service)
    {
        var controller = new AdvisorController(service, NullLogger<AdvisorController>.Instance);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(
                    new ClaimsIdentity(
                        [new Claim(ClaimTypes.NameIdentifier, "7")],
                        "Test"))
            }
        };
        return controller;
    }

    private sealed class StubAdvisorService : ICollectionAdvisorService
    {
        public Exception? SendFailure { get; set; }

        public Task<AdvisorChatStateDto> GetCurrentStateAsync(
            int userId,
            CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<AdvisorChatStateDto?> GetStateAsync(
            int sessionId,
            int userId,
            CancellationToken ct = default) =>
            Task.FromResult<AdvisorChatStateDto?>(null);

        public Task<AdvisorChatStateDto> StartNewSessionAsync(
            int userId,
            CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<AdvisorChatStateDto?> SendMessageAsync(
            int sessionId,
            int userId,
            SendAdvisorMessageDto dto,
            CancellationToken ct = default)
        {
            if (SendFailure is not null)
                return Task.FromException<AdvisorChatStateDto?>(SendFailure);
            return Task.FromResult<AdvisorChatStateDto?>(null);
        }

        public Task<AdvisorRecommendationFeedbackDto?> SaveFeedbackAsync(
            int messageId,
            int userId,
            SaveAdvisorFeedbackDto dto,
            CancellationToken ct = default) =>
            Task.FromResult<AdvisorRecommendationFeedbackDto?>(null);

        public Task<bool> RemoveFeedbackAsync(
            int feedbackId,
            int userId,
            CancellationToken ct = default) =>
            Task.FromResult(false);

        public Task<AdvisorWishlistActionResultDto?> AddToWishlistAsync(
            int messageId,
            int userId,
            AdvisorRecommendationActionDto dto,
            CancellationToken ct = default) =>
            Task.FromResult<AdvisorWishlistActionResultDto?>(null);
    }
}
