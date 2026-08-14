using Microsoft.EntityFrameworkCore;
using WatchTracker.Api.Data;
using WatchTracker.Api.DTOs;
using WatchTracker.Api.Models;

namespace WatchTracker.Api.Services;

public class ResaleValueRefreshService(
    AppDbContext context,
    IAppSettingsService appSettings,
    IEnumerable<IResaleValueEstimator> estimators,
    IWatchService watchService,
    ILogger<ResaleValueRefreshService> logger) : IResaleValueRefreshService
{
    private const int MinRefreshIntervalDays = 7;

    public async Task<WatchDto?> RefreshWatchAsync(int watchId, int userId, CancellationToken ct = default)
    {
        var watch = await context.Watches
            .FirstOrDefaultAsync(w => w.Id == watchId && w.UserId == userId, ct);

        if (watch is null) return null;

        var (succeeded, error) = await RefreshWatchInternalAsync(watch, ct);
        if (!succeeded)
            throw new InvalidOperationException(error ?? "No resale value source is configured or returned a result.");

        return await watchService.GetByIdAsync(watchId, userId, ct);
    }

    public async Task<int> RefreshDueWatchesAsync(CancellationToken ct = default)
    {
        var intervalDays = Math.Max(
            MinRefreshIntervalDays,
            await appSettings.GetIntAsync(AppSettingsService.Keys.ResaleValueRefreshIntervalDays, MinRefreshIntervalDays));
        var cutoff = DateTime.UtcNow.AddDays(-intervalDays);

        var dueWatches = await context.Watches
            .Where(w => w.Disposition == null && !w.IsWishList)
            .Where(w => w.ResaleValueUpdatedAt == null || w.ResaleValueUpdatedAt < cutoff)
            .ToListAsync(ct);

        var refreshed = 0;
        foreach (var watch in dueWatches)
        {
            try
            {
                var (succeeded, _) = await RefreshWatchInternalAsync(watch, ct);
                if (succeeded) refreshed++;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Scheduled resale value refresh failed for watch {WatchId}.", watch.Id);
            }
        }

        return refreshed;
    }

    public async Task<ResaleRefreshSummaryDto> RefreshAllNowAsync(CancellationToken ct = default)
    {
        var watches = await context.Watches
            .Where(w => w.Disposition == null && !w.IsWishList)
            .ToListAsync(ct);

        var summary = new ResaleRefreshSummaryDto { Due = watches.Count };

        foreach (var watch in watches)
        {
            try
            {
                var (succeeded, _) = await RefreshWatchInternalAsync(watch, ct);
                if (succeeded) summary.Refreshed++;
                else summary.Skipped++;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                summary.Failed++;
                logger.LogWarning(ex, "Manual resale value refresh failed for watch {WatchId}.", watch.Id);
            }
        }

        return summary;
    }

    private async Task<(bool Succeeded, string? Error)> RefreshWatchInternalAsync(Watch watch, CancellationToken ct)
    {
        var estimateResults = await Task.WhenAll(estimators.Select(e => e.EstimateAsync(watch, ct)));
        var results = estimateResults.Where(r => r is not null).Select(r => r!).ToList();

        if (results.Count == 0)
            return (false, "No resale value source is configured or returned a result.");

        var average = results.Average(r => r.Value);
        var combinedReasoning = string.Join(" | ", results.Select(r => $"{r.SourceName}: {r.Reasoning}"));

        var entry = new ResaleValueEntry
        {
            WatchId = watch.Id,
            UserId = watch.UserId,
            Value = average,
            Source = ResaleValueSource.WebSearchEstimate,
            Reasoning = combinedReasoning,
            RecordedAt = DateTime.UtcNow,
        };
        context.ResaleValueEntries.Add(entry);

        watch.CurrentResaleValue = average;
        watch.ResaleValueUpdatedAt = entry.RecordedAt;
        watch.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);
        return (true, null);
    }
}
