using System.Text.Json;
using WatchTracker.Api.Serialization;

namespace WatchTracker.Api.Tests;

public class UtcDateTimeJsonConverterTests
{
    [Fact]
    public void Write_marks_unspecified_sqlite_timestamp_as_utc()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new UtcDateTimeJsonConverter());
        var value = new DateTime(2025, 8, 19, 1, 32, 0, DateTimeKind.Unspecified);

        var json = JsonSerializer.Serialize(value, options);

        Assert.Equal("\"2025-08-19T01:32:00Z\"", json);
    }

    [Fact]
    public void Read_preserves_explicit_utc_timestamp()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new UtcDateTimeJsonConverter());

        var value = JsonSerializer.Deserialize<DateTime>(
            "\"2025-08-19T01:32:00Z\"",
            options);

        Assert.Equal(DateTimeKind.Utc, value.Kind);
        Assert.Equal(new DateTime(2025, 8, 19, 1, 32, 0, DateTimeKind.Utc), value);
    }
}
