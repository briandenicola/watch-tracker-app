namespace WatchTracker.Api.Services;

public class PriceScanBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<PriceScanBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(CheckInterval);
        do
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var scanner = scope.ServiceProvider.GetRequiredService<IWishlistPriceScanner>();
                var scanned = await scanner.ScanDueAsync(stoppingToken);
                if (scanned > 0)
                    logger.LogInformation("Scheduled price scan completed for {Count} wish list watch(es).", scanned);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Scheduled price scan pass failed.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
