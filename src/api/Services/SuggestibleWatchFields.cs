using WatchTracker.Api.Models;

namespace WatchTracker.Api.Services;

/// <summary>
/// One watch field the AI analysis is allowed to propose a value for.
/// </summary>
public sealed class SuggestibleWatchField
{
    public required string Name { get; init; }
    public required string Label { get; init; }

    /// <summary>"text", "number" or "integer" — tells the UI which input to render.</summary>
    public required string Kind { get; init; }

    /// <summary>What to tell the model this field means, so it answers in the right shape.</summary>
    public required string Hint { get; init; }

    public required Func<Watch, bool> IsMissing { get; init; }

    /// <summary>Writes the value, or returns why it could not be used.</summary>
    public required Func<Watch, string, string?> Apply { get; init; }
}

/// <summary>
/// The allow-list of fields the analysis may fill in. Everything here is
/// something a good photo plus knowledge of the brand can reasonably settle.
/// Deliberately absent: serial number, anything about money or provenance,
/// storage location, and notes — the model has no business guessing those, and
/// a wrong guess there is worse than a blank.
/// </summary>
public static class SuggestibleWatchFields
{
    // Mirrors the limits on CreateWatchDto, so an approved value can never be
    // one the ordinary edit form would have rejected.
    private const int MaxTextLength = 100;

    public static readonly IReadOnlyList<SuggestibleWatchField> All =
    [
        Text("sku", "SKU / Reference", "the manufacturer's reference number, if you recognise the model",
            w => w.Sku, (w, v) => w.Sku = v),
        Text("dialColor", "Dial Color", "the dial's colour in one or two words",
            w => w.DialColor, (w, v) => w.DialColor = v),
        Text("caseShape", "Case Shape", "round, cushion, tonneau, rectangular, and so on",
            w => w.CaseShape, (w, v) => w.CaseShape = v),
        Text("bezelType", "Bezel", "e.g. dive/rotating, fixed, tachymeter, GMT",
            w => w.BezelType, (w, v) => w.BezelType = v),
        Text("crystalType", "Crystal", "sapphire, mineral, acrylic — only if you can tell",
            w => w.CrystalType, (w, v) => w.CrystalType = v),
        Text("crownType", "Crown", "e.g. push-pull, screw-down, crown guards",
            w => w.CrownType, (w, v) => w.CrownType = v),
        Text("calendarType", "Calendar", "e.g. date, day-date, none",
            w => w.CalendarType, (w, v) => w.CalendarType = v),
        Text("bandType", "Band Type", "e.g. leather, steel bracelet, rubber, NATO",
            w => w.BandType, (w, v) => w.BandType = v),
        Text("bandColor", "Band Color", "the strap or bracelet colour",
            w => w.BandColor, (w, v) => w.BandColor = v),
        Text("waterResistance", "Water Resistance", "as printed on the dial or case, e.g. 100m",
            w => w.WaterResistance, (w, v) => w.WaterResistance = v),
        Text("countryOfOrigin", "Origin", "the country the brand builds in, if you know it",
            w => w.CountryOfOrigin, (w, v) => w.CountryOfOrigin = v),
        Text("batteryType", "Battery Type", "only for a quartz watch, e.g. SR626SW",
            w => w.BatteryType, (w, v) => w.BatteryType = v),

        Number("caseSizeMm", "Case Size", "case diameter in millimetres", 1, 200,
            w => w.CaseSizeMm, (w, v) => w.CaseSizeMm = v),
        Number("lugWidthMm", "Lug Width", "lug width in millimetres", 1, 100,
            w => w.LugWidthMm, (w, v) => w.LugWidthMm = v),

        Integer("powerReserveHours", "Power Reserve", "power reserve in hours, for a mechanical movement", 0, 10000,
            w => w.PowerReserveHours, (w, v) => w.PowerReserveHours = v),
        Integer("productionYear", "Production Year", "the year this reference was made, if you are confident", 1800, 2200,
            w => w.ProductionYear, (w, v) => w.ProductionYear = v),
    ];

    public static SuggestibleWatchField? Find(string name) =>
        All.FirstOrDefault(f => string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase));

    /// <summary>The fields this watch has no value for yet.</summary>
    public static List<SuggestibleWatchField> MissingOn(Watch watch) =>
        All.Where(f => f.IsMissing(watch)).ToList();

    private static SuggestibleWatchField Text(
        string name, string label, string hint, Func<Watch, string?> get, Action<Watch, string> set) => new()
        {
            Name = name,
            Label = label,
            Kind = "text",
            Hint = hint,
            IsMissing = w => string.IsNullOrWhiteSpace(get(w)),
            Apply = (w, value) =>
            {
                var trimmed = value.Trim();
                if (trimmed.Length == 0) return "is empty";
                if (trimmed.Length > MaxTextLength) return $"is longer than {MaxTextLength} characters";
                set(w, trimmed);
                return null;
            }
        };

    private static SuggestibleWatchField Number(
        string name, string label, string hint, double min, double max,
        Func<Watch, double?> get, Action<Watch, double?> set) => new()
        {
            Name = name,
            Label = label,
            Kind = "number",
            Hint = hint,
            IsMissing = w => get(w) is null,
            Apply = (w, value) =>
            {
                if (!double.TryParse(value.Trim(), System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out var parsed))
                    return "is not a number";
                if (parsed < min || parsed > max) return $"is outside {min}–{max}";
                set(w, parsed);
                return null;
            }
        };

    private static SuggestibleWatchField Integer(
        string name, string label, string hint, int min, int max,
        Func<Watch, int?> get, Action<Watch, int?> set) => new()
        {
            Name = name,
            Label = label,
            Kind = "integer",
            Hint = hint,
            IsMissing = w => get(w) is null,
            Apply = (w, value) =>
            {
                if (!int.TryParse(value.Trim(), System.Globalization.NumberStyles.Integer,
                        System.Globalization.CultureInfo.InvariantCulture, out var parsed))
                    return "is not a whole number";
                if (parsed < min || parsed > max) return $"is outside {min}–{max}";
                set(w, parsed);
                return null;
            }
        };
}
