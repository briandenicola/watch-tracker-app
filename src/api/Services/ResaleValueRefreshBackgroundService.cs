namespace WatchTracker.Api.Services;

public class ResaleValueRefreshBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<ResaleValueRefreshBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(6);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(CheckInterval);
        do
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var refreshService = scope.ServiceProvider.GetRequiredService<IResaleValueRefreshService>();
                var count = await refreshService.RefreshDueWatchesAsync(stoppingToken);
                if (count > 0)
                    logger.LogInformation("Resale value refresh: updated {Count} watch(es).", count);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Resale value refresh background pass failed.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
