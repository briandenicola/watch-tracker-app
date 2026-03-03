namespace WatchTracker.Api.Services;

/// <summary>
/// A configuration provider that can be updated at runtime and triggers
/// reload notifications so that IOptionsMonitor-based consumers (like logging)
/// pick up changes immediately.
/// </summary>
public class DynamicConfigurationSource : Microsoft.Extensions.Configuration.IConfigurationSource
{
    public DynamicConfigurationProvider Provider { get; } = new();

    public Microsoft.Extensions.Configuration.IConfigurationProvider Build(
        Microsoft.Extensions.Configuration.IConfigurationBuilder builder) => Provider;
}

public class DynamicConfigurationProvider : Microsoft.Extensions.Configuration.ConfigurationProvider
{
    public new void Set(string key, string value)
    {
        Data[key] = value;
        OnReload();
    }
}
