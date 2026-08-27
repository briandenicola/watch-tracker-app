using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Builder;
using WatchTracker.Api.Configuration;

namespace WatchTracker.Api.Tests;

public class TrustedProxyNetworksTests
{
    [Fact]
    public void Empty_configuration_trusts_no_forwarded_header_sources()
    {
        var options = new ForwardedHeadersOptions();

        TrustedProxyNetworks.Configure(options, "");

        Assert.Empty(options.KnownIPNetworks);
        Assert.Empty(options.KnownProxies);
    }

    [Fact]
    public void Configuration_accepts_semicolon_separated_ip_and_cidr_entries()
    {
        var options = new ForwardedHeadersOptions();

        TrustedProxyNetworks.Configure(options, "10.0.0.5; 172.18.0.0/16");

        Assert.Equal(2, options.KnownIPNetworks.Count);
        Assert.Equal(1, options.ForwardLimit);
    }

    [Theory]
    [InlineData("not-a-network")]
    [InlineData("10.0.0.1/33")]
    [InlineData("::1/129")]
    public void Configuration_rejects_invalid_entries(string value)
    {
        var options = new ForwardedHeadersOptions();

        Assert.Throws<InvalidOperationException>(() =>
            TrustedProxyNetworks.Configure(options, value));
    }
}
