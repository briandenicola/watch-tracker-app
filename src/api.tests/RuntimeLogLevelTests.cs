using WatchTracker.Api.Services;

namespace WatchTracker.Api.Tests;

public class RuntimeLogLevelTests
{
    [Fact]
    public void Verbose_levels_reach_the_framework_categories()
    {
        var provider = new DynamicConfigurationProvider();

        RuntimeLogLevel.Apply(provider, "Debug");

        // appsettings.json pins Microsoft.AspNetCore to Warning, so moving only the
        // default category left request-level diagnostics silent at Debug — which is
        // where an abandoned or rate-limited agent request is recorded.
        Assert.True(provider.TryGet(RuntimeLogLevel.DefaultKey, out var defaultLevel));
        Assert.Equal("Debug", defaultLevel);
        Assert.True(provider.TryGet(RuntimeLogLevel.AspNetCoreKey, out var frameworkLevel));
        Assert.Equal("Debug", frameworkLevel);
    }

    [Theory]
    [InlineData("Information")]
    [InlineData("Warning")]
    [InlineData("Error")]
    public void Ordinary_levels_keep_the_framework_quiet(string level)
    {
        var provider = new DynamicConfigurationProvider();

        RuntimeLogLevel.Apply(provider, level);

        Assert.True(provider.TryGet(RuntimeLogLevel.DefaultKey, out var defaultLevel));
        Assert.Equal(level, defaultLevel);
        Assert.True(provider.TryGet(RuntimeLogLevel.AspNetCoreKey, out var frameworkLevel));
        Assert.Equal("Warning", frameworkLevel);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void A_missing_setting_falls_back_to_information(string? level)
    {
        var provider = new DynamicConfigurationProvider();

        RuntimeLogLevel.Apply(provider, level);

        Assert.True(provider.TryGet(RuntimeLogLevel.DefaultKey, out var defaultLevel));
        Assert.Equal("Information", defaultLevel);
    }
}
