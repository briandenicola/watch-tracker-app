using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using WatchTracker.Api.Controllers;
using WatchTracker.Api.DTOs;
using WatchTracker.Api.Services;

namespace WatchTracker.Api.Tests;

public class PriceMonitoringControllerTests
{
    [Fact]
    public async Task Scan_returns_not_found_when_the_scanner_cannot_find_an_owned_watch()
    {
        var controller = CreateController(new StubScanner());

        var result = await controller.ScanWishlistPrice(42, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Scan_returns_bad_request_for_a_non_wish_list_watch()
    {
        var controller = CreateController(new StubScanner
        {
            ScanError = new InvalidOperationException(
                "Price scanning is available only for active wish list watches.")
        });

        var result = await controller.ScanWishlistPrice(42, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("wish list", badRequest.Value!.ToString());
    }

    [Fact]
    public async Task Observation_history_returns_not_found_when_the_item_is_not_owned()
    {
        var controller = CreateController(new StubScanner());

        var result = await controller.GetPriceObservations(42, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Marking_another_users_alert_read_returns_not_found()
    {
        var controller = CreateController(new StubScanner(), new StubAlerts { Marked = false });

        var result = await controller.MarkPriceAlertRead(9, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Marking_all_alerts_read_uses_the_authenticated_owner()
    {
        var alerts = new StubAlerts();
        var controller = CreateController(new StubScanner(), alerts);

        var result = await controller.MarkAllPriceAlertsRead(CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal(7, alerts.MarkAllReadUserId);
    }

    private static WatchesController CreateController(
        IWishlistPriceScanner scanner,
        IPriceAlertService? alerts = null)
    {
        var controller = new WatchesController(
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            scanner,
            alerts ?? new StubAlerts(),
            null!,
            NullLogger<WatchesController>.Instance);
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

    private sealed class StubScanner : IWishlistPriceScanner
    {
        public Exception? ScanError { get; set; }

        public Task<PriceScanResultDto?> ScanAsync(int watchId, int userId, CancellationToken ct = default)
        {
            if (ScanError is not null)
                return Task.FromException<PriceScanResultDto?>(ScanError);
            return Task.FromResult<PriceScanResultDto?>(null);
        }

        public Task<int> ScanDueAsync(CancellationToken ct = default) => Task.FromResult(0);

        public Task<IReadOnlyList<PriceObservationDto>?> GetObservationsAsync(
            int watchId,
            int userId,
            CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<PriceObservationDto>?>(null);

        public Task<PriceMonitoringDto?> UpdateMonitoringAsync(
            int watchId,
            int userId,
            UpdatePriceMonitoringDto dto,
            CancellationToken ct = default) =>
            Task.FromResult<PriceMonitoringDto?>(null);
    }

    private sealed class StubAlerts : IPriceAlertService
    {
        public bool Marked { get; set; }
        public int? MarkAllReadUserId { get; private set; }

        public Task<IReadOnlyList<PriceAlertDto>> GetAlertsAsync(
            int userId,
            bool unreadOnly,
            CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<PriceAlertDto>>([]);

        public Task<bool> MarkReadAsync(int alertId, int userId, CancellationToken ct = default) =>
            Task.FromResult(Marked);

        public Task<int> MarkAllReadAsync(int userId, CancellationToken ct = default)
        {
            MarkAllReadUserId = userId;
            return Task.FromResult(0);
        }
    }
}
