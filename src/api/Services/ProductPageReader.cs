using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace WatchTracker.Api.Services;

/// <summary>
/// Fetches a watch's product or store page so the analysis can read the specs
/// off it rather than guess them.
///
/// The URL comes from a user, which makes this the one place in the app that
/// asks the server to fetch wherever it is told. It is therefore fenced in:
/// http(s) only, redirects followed by hand and re-checked at every hop, and —
/// via <see cref="CreateHandler"/> — connections opened only to public IP
/// addresses, checked at connect time so a name that resolves differently on
/// the second lookup cannot slip past.
/// </summary>
public class ProductPageReader(HttpClient httpClient, ILogger<ProductPageReader> logger) : IProductPageReader
{
    private const int MaxBytes = 512 * 1024;
    private const int MaxRedirects = 3;
    private const int MaxTextLength = 2500;
    private const int MaxJsonLdLength = 12000;

    private static readonly Regex ScriptOrStyle = new(
        @"<(script|style|noscript|svg)\b[^>]*>.*?</\1>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline, TimeSpan.FromSeconds(2));

    private static readonly Regex Comments = new(
        @"<!--.*?-->", RegexOptions.Singleline, TimeSpan.FromSeconds(2));

    private static readonly Regex Tags = new(
        @"<[^>]+>", RegexOptions.None, TimeSpan.FromSeconds(2));

    private static readonly Regex Whitespace = new(
        @"\s+", RegexOptions.None, TimeSpan.FromSeconds(2));

    private static readonly Regex TitleTag = new(
        @"<title\b[^>]*>(.*?)</title>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline, TimeSpan.FromSeconds(2));

    private static readonly Regex JsonLdTag = new(
        @"<script\b[^>]*\btype\s*=\s*[""']application/ld\+json[""'][^>]*>(.*?)</script>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline, TimeSpan.FromSeconds(2));

    private static readonly Regex MetaTag = new(
        @"<meta\b[^>]*>", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(2));

    private static readonly Regex Attribute = new(
        @"(?<name>[\w:-]+)\s*=\s*(?:""(?<value>[^""]*)""|'(?<value>[^']*)'|(?<value>[^\s>]+))",
        RegexOptions.IgnoreCase, TimeSpan.FromSeconds(2));

    /// <summary>
    /// The handler this reader must be registered with: it resolves each host at
    /// connect time and refuses anything that is not a public address, which is
    /// what keeps a user-supplied link from reaching the machine's own network.
    /// </summary>
    public static SocketsHttpHandler CreateHandler() => new()
    {
        AllowAutoRedirect = false,
        UseProxy = false,
        ConnectCallback = async (context, ct) =>
        {
            var addresses = await Dns.GetHostAddressesAsync(context.DnsEndPoint.Host, ct);
            var target = Array.Find(addresses, IsPublicAddress)
                ?? throw new HttpRequestException(
                    $"Refusing to connect to {context.DnsEndPoint.Host}: it does not resolve to a public address.");

            var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
            try
            {
                await socket.ConnectAsync(new IPEndPoint(target, context.DnsEndPoint.Port), ct);
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch
            {
                socket.Dispose();
                throw;
            }
        }
    };

    public async Task<LinkedPageExcerpt?> ReadAsync(string url, CancellationToken ct = default)
    {
        if (!TryParseWebUrl(url, out var uri)) return null;

        try
        {
            for (var hop = 0; hop <= MaxRedirects; hop++)
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, uri);
                // Some storefronts serve a stub to clients that ask for nothing.
                request.Headers.TryAddWithoutValidation("Accept", "text/html,application/xhtml+xml");

                using var response = await httpClient.SendAsync(
                    request, HttpCompletionOption.ResponseHeadersRead, ct);

                if (IsRedirect(response.StatusCode))
                {
                    var location = response.Headers.Location;
                    if (location is null) return null;

                    var next = location.IsAbsoluteUri ? location : new Uri(uri, location);
                    if (!TryParseWebUrl(next.ToString(), out uri)) return null;
                    continue;
                }

                if (!response.IsSuccessStatusCode) return null;

                var mediaType = response.Content.Headers.ContentType?.MediaType;
                if (mediaType is not ("text/html" or "application/xhtml+xml" or "text/plain"))
                    return null;

                var html = await ReadCappedAsync(response, ct);
                var text = ExtractText(html);
                if (text.Length == 0) return null;

                return new LinkedPageExcerpt(
                    uri.ToString(),
                    ExtractTitle(html),
                    text,
                    ExtractMetadata(html, uri),
                    ExtractJsonLd(html));
            }

            return null;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // A dead link, a blocked host, a timeout — all the same to the caller.
            logger.LogInformation("Could not read the linked page {Url} for analysis: {Reason}", url, ex.Message);
            return null;
        }
    }

    private static bool IsRedirect(HttpStatusCode status) =>
        status is HttpStatusCode.Moved or HttpStatusCode.Found or HttpStatusCode.SeeOther
            or HttpStatusCode.TemporaryRedirect or HttpStatusCode.PermanentRedirect;

    private static bool TryParseWebUrl(string url, out Uri uri)
    {
        uri = null!;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var parsed)) return false;
        if (parsed.Scheme != Uri.UriSchemeHttp && parsed.Scheme != Uri.UriSchemeHttps) return false;

        uri = parsed;
        return true;
    }

    private static async Task<string> ReadCappedAsync(HttpResponseMessage response, CancellationToken ct)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(ct);

        var buffer = new byte[MaxBytes];
        var filled = 0;
        while (filled < MaxBytes)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(filled, MaxBytes - filled), ct);
            if (read == 0) break;
            filled += read;
        }

        return Encoding.UTF8.GetString(buffer, 0, filled);
    }

    private static string ExtractText(string html)
    {
        try
        {
            var text = ScriptOrStyle.Replace(html, " ");
            text = Comments.Replace(text, " ");
            text = Tags.Replace(text, " ");
            text = WebUtility.HtmlDecode(text);
            text = Whitespace.Replace(text, " ").Trim();

            return text.Length <= MaxTextLength ? text : text[..MaxTextLength];
        }
        catch (RegexMatchTimeoutException)
        {
            return "";
        }
    }

    private static string? ExtractTitle(string html)
    {
        try
        {
            var match = TitleTag.Match(html);
            if (!match.Success) return null;

            var title = Whitespace.Replace(WebUtility.HtmlDecode(match.Groups[1].Value), " ").Trim();
            return title.Length == 0 ? null : title[..Math.Min(title.Length, 200)];
        }
        catch (RegexMatchTimeoutException)
        {
            return null;
        }
    }

    private static IReadOnlyDictionary<string, string> ExtractMetadata(string html, Uri pageUri)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            foreach (Match tag in MetaTag.Matches(html))
            {
                var attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (Match attribute in Attribute.Matches(tag.Value))
                    attributes.TryAdd(
                        attribute.Groups["name"].Value,
                        WebUtility.HtmlDecode(attribute.Groups["value"].Value).Trim());

                if (!attributes.TryGetValue("property", out var key)
                    && !attributes.TryGetValue("name", out key))
                    continue;
                if (!attributes.TryGetValue("content", out var value)
                    || string.IsNullOrWhiteSpace(key)
                    || string.IsNullOrWhiteSpace(value))
                    continue;

                if (key is "og:image" or "twitter:image"
                    && Uri.TryCreate(pageUri, value, out var imageUri))
                    value = imageUri.ToString();

                result.TryAdd(key.Trim().ToLowerInvariant(), value);
            }
        }
        catch (RegexMatchTimeoutException)
        {
            return result;
        }

        return result;
    }

    private static IReadOnlyList<string> ExtractJsonLd(string html)
    {
        var result = new List<string>();
        var remaining = MaxJsonLdLength;
        try
        {
            foreach (Match match in JsonLdTag.Matches(html))
            {
                var value = WebUtility.HtmlDecode(match.Groups[1].Value).Trim();
                if (value.Length == 0) continue;

                var bounded = value[..Math.Min(value.Length, remaining)];
                result.Add(bounded);
                remaining -= bounded.Length;
                if (remaining == 0) break;
            }
        }
        catch (RegexMatchTimeoutException)
        {
            return result;
        }

        return result;
    }

    private static bool IsPublicAddress(IPAddress address)
    {
        if (IPAddress.IsLoopback(address)) return false;

        var candidate = address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address;

        if (candidate.AddressFamily == AddressFamily.InterNetwork)
        {
            var b = candidate.GetAddressBytes();
            return b[0] switch
            {
                0 or 10 or 127 => false,
                100 when b[1] >= 64 && b[1] <= 127 => false,  // carrier-grade NAT
                169 when b[1] == 254 => false,                 // link-local, incl. cloud metadata
                172 when b[1] >= 16 && b[1] <= 31 => false,
                192 when b[1] == 168 => false,
                >= 224 => false,                               // multicast and reserved
                _ => true
            };
        }

        if (candidate.AddressFamily == AddressFamily.InterNetworkV6)
        {
            if (candidate.IsIPv6LinkLocal || candidate.IsIPv6SiteLocal || candidate.IsIPv6Multicast) return false;
            if (candidate.Equals(IPAddress.IPv6Loopback)) return false;

            // fc00::/7 — unique local
            return (candidate.GetAddressBytes()[0] & 0xFE) != 0xFC;
        }

        return false;
    }
}
