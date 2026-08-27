using Microsoft.Extensions.Diagnostics.HealthChecks;
using WatchTracker.Api.Diagnostics;

namespace WatchTracker.Api.Tests;

public class DatabaseHealthCheckTests
{
    [Fact]
    public async Task Ready_when_the_database_connection_is_available()
    {
        await using var database = await TestDatabase.CreateAsync();
        var healthCheck = new DatabaseHealthCheck(database.Context);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, result.Status);
    }
}
