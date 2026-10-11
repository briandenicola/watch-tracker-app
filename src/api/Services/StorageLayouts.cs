using System.Text.Json;
using WatchTracker.Api.DTOs;

namespace WatchTracker.Api.Services;

internal static class StorageLayouts
{
    public static Dictionary<string, StorageLayoutDto> Parse(string? json)
    {
        var result = new Dictionary<string, StorageLayoutDto>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(json))
            return result;

        try
        {
            var parsed = JsonSerializer.Deserialize<Dictionary<string, StorageLayoutDto>>(json) ?? [];
            foreach (var (name, layout) in parsed)
            {
                if (IsValid(layout))
                    result[name] = layout;
            }
        }
        catch (JsonException)
        {
        }

        return result;
    }

    public static bool IsValid(StorageLayoutDto? layout) =>
        layout is { Rows: >= 1 and <= StorageLayoutLimits.MaxDimension, Columns: >= 1 and <= StorageLayoutLimits.MaxDimension };

    public static int Capacity(StorageLayoutDto layout) => layout.Rows * layout.Columns;
}
