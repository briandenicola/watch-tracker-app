using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace WatchTracker.Api.Configuration;

public static class TrustedProxyNetworks
{
    public static void Configure(ForwardedHeadersOptions options, string? configuredNetworks)
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.ForwardLimit = 1;
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();

        if (string.IsNullOrWhiteSpace(configuredNetworks)) return;

        foreach (var value in configuredNetworks.Split(
                     ';',
                     StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            options.KnownIPNetworks.Add(Parse(value));
        }
    }

    private static System.Net.IPNetwork Parse(string value)
    {
        var parts = value.Split('/', StringSplitOptions.TrimEntries);
        if (parts.Length is < 1 or > 2 || !IPAddress.TryParse(parts[0], out var address))
            throw new InvalidOperationException(
                $"ForwardedHeaders:TrustedNetworks contains invalid IP network '{value}'.");

        var maximumPrefix = address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork ? 32 : 128;
        var prefixLength = parts.Length == 1
            ? maximumPrefix
            : int.TryParse(parts[1], out var parsed) ? parsed : -1;
        if (prefixLength < 0 || prefixLength > maximumPrefix)
            throw new InvalidOperationException(
                $"ForwardedHeaders:TrustedNetworks contains invalid CIDR prefix '{value}'.");

        return new System.Net.IPNetwork(address, prefixLength);
    }
}
