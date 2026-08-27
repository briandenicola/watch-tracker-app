using Microsoft.Extensions.Diagnostics.HealthChecks;
using WatchTracker.Api.Data;

namespace WatchTracker.Api.Diagnostics;

public sealed class DatabaseHealthCheck(AppDbContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken ct = default) =>
        await db.Database.CanConnectAsync(ct)
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("The application database cannot be reached.");
}
