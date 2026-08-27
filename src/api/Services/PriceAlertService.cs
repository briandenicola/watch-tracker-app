using Microsoft.EntityFrameworkCore;
using WatchTracker.Api.Data;
using WatchTracker.Api.DTOs;
using WatchTracker.Api.Models;

namespace WatchTracker.Api.Services;

public class PriceAlertService(AppDbContext context, ILogger<PriceAlertService> logger)
    : IPriceAlertEvaluator, IPriceAlertService
{
    public async Task<int> EvaluateAsync(
        PriceObservation observation,
        Watch watch,
        CancellationToken ct = default)
    {
        // This is deliberately enforced here as well as in the scanner. Any
        // future producer of observations must not turn an uncertain match into
        // an alert merely by calling this service directly.
        if (!watch.PriceAlertEnabled
            || observation.MatchConfidence != PriceMatchConfidence.High
            || observation.WatchId != watch.Id
            || observation.UserId != watch.UserId)
            return 0;

        var triggers = new List<PriceAlertTrigger>();
        if (watch.PriceAlertTarget is decimal target && observation.Price < target)
            triggers.Add(PriceAlertTrigger.BelowTarget);

        var previousPrices = await context.PriceObservations
            .AsNoTracking()
            .Where(o => o.WatchId == observation.WatchId
                && o.Id != observation.Id
                && o.MatchConfidence == PriceMatchConfidence.High)
            .Select(o => o.Price)
            .ToListAsync(ct);
        if (previousPrices.Count > 0 && observation.Price < previousPrices.Min())
            triggers.Add(PriceAlertTrigger.NewBest);

        if (triggers.Count == 0) return 0;

        var existing = await context.PriceAlerts
            .Where(a => a.PriceObservationId == observation.Id)
            .Select(a => a.Trigger)
            .ToListAsync(ct);
        var newTriggers = triggers.Except(existing).ToList();
        foreach (var trigger in newTriggers)
        {
            context.PriceAlerts.Add(new PriceAlert
            {
                PriceObservationId = observation.Id,
                UserId = observation.UserId,
                Trigger = trigger,
                CreatedAt = DateTime.UtcNow
            });
        }

        try
        {
            await context.SaveChangesAsync(ct);
            return newTriggers.Count;
        }
        catch (DbUpdateException)
        {
            // The unique observation/rule index makes concurrent scans
            // idempotent. A competing request has already created the alert.
            foreach (var entry in context.ChangeTracker.Entries<PriceAlert>()
                         .Where(e => e.State == EntityState.Added))
                entry.State = EntityState.Detached;
            logger.LogInformation(
                "A duplicate price alert was suppressed for observation {ObservationId}.",
                observation.Id);
            return 0;
        }
    }

    public async Task<IReadOnlyList<PriceAlertDto>> GetAlertsAsync(
        int userId,
        bool unreadOnly,
        CancellationToken ct = default)
    {
        var query = context.PriceAlerts
            .AsNoTracking()
            .Include(a => a.PriceObservation)
                .ThenInclude(o => o.Watch)
            .Where(a => a.UserId == userId);
        if (unreadOnly) query = query.Where(a => !a.IsRead);

        var alerts = await query
            .OrderBy(a => a.IsRead)
            .ThenByDescending(a => a.CreatedAt)
            .Take(100)
            .ToListAsync(ct);
        return alerts.Select(Map).ToList();
    }

    public async Task<bool> MarkReadAsync(int alertId, int userId, CancellationToken ct = default)
    {
        var alert = await context.PriceAlerts
            .FirstOrDefaultAsync(a => a.Id == alertId && a.UserId == userId, ct);
        if (alert is null) return false;

        if (!alert.IsRead)
        {
            alert.IsRead = true;
            alert.ReadAt = DateTime.UtcNow;
            await context.SaveChangesAsync(ct);
        }

        return true;
    }

    internal static PriceAlertDto Map(PriceAlert alert) => new()
    {
        Id = alert.Id,
        WatchId = alert.PriceObservation.WatchId,
        WatchBrand = alert.PriceObservation.Watch.Brand,
        WatchModel = alert.PriceObservation.Watch.Model,
        Trigger = alert.Trigger,
        IsRead = alert.IsRead,
        ReadAt = alert.ReadAt,
        CreatedAt = alert.CreatedAt,
        Observation = WishlistPriceScanner.Map(alert.PriceObservation)
    };
}
