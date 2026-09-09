using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using WatchTracker.Api.Data;
using WatchTracker.Api.DTOs;
using WatchTracker.Api.Models;

namespace WatchTracker.Api.Services;

public class ExternalDataImportService(
    AppDbContext context,
    ILogger<ExternalDataImportService> logger) : IExternalDataImportService
{
    private const int MaxRows = 5000;
    private const long MaxFileBytes = 5 * 1024 * 1024;

    public async Task<ExternalImportPreviewDto> PreviewAsync(
        int userId,
        IFormFile file,
        CancellationToken ct = default)
    {
        var rows = await ParseAsync(file, ct);
        var existing = await context.Watches
            .Where(watch => watch.UserId == userId)
            .Select(watch => new ExistingWatch(
                watch.Id,
                watch.Brand,
                watch.Model,
                watch.Sku,
                watch.SerialNumber))
            .ToListAsync(ct);

        var seen = new List<ExistingWatch>(existing);
        var previewRows = new List<ExternalImportRowDto>(rows.Count);
        foreach (var row in rows)
        {
            var duplicate = FindDuplicate(row, seen);
            var preview = ToPreviewRow(row, duplicate);
            previewRows.Add(preview);
            if (preview.CanImport)
                seen.Add(new ExistingWatch(0, row.Watch.Brand, row.Watch.Model, row.Watch.Sku, row.Watch.SerialNumber));
        }

        return new ExternalImportPreviewDto
        {
            Source = "Wristcheck",
            CollectionCount = rows.Count(row => !row.Watch.IsWishList && row.Disposition is null),
            WishlistCount = rows.Count(row => row.Watch.IsWishList),
            DisposedCount = rows.Count(row => row.Disposition is not null),
            DuplicateCount = previewRows.Count(row => row.IsDuplicate),
            Rows = previewRows,
        };
    }

    public async Task<ExternalImportResultDto> ImportAsync(
        int userId,
        IFormFile file,
        IReadOnlySet<int> selectedRows,
        CancellationToken ct = default)
    {
        var rows = await ParseAsync(file, ct);
        var selected = rows.Where(row => selectedRows.Contains(row.RowNumber)).ToList();
        if (selected.Count == 0)
            throw new InvalidDataException("Select at least one valid row to import.");

        var invalid = selected.FirstOrDefault(row => row.Errors.Count > 0);
        if (invalid is not null)
            throw new InvalidDataException($"CSV row {invalid.RowNumber}: {invalid.Errors[0]}");

        var priorityLock = selected.Any(row => row.Watch.IsWishList)
            ? WishlistPriorityLocks.ForUser(userId)
            : null;
        if (priorityLock is not null)
            await priorityLock.WaitAsync(ct);

        try
        {
            var existing = await context.Watches
                .Where(watch => watch.UserId == userId)
                .Select(watch => new ExistingWatch(
                    watch.Id,
                    watch.Brand,
                    watch.Model,
                    watch.Sku,
                    watch.SerialNumber))
                .ToListAsync(ct);
            var seen = existing.ToList();
            var wishlistPriority = (await context.Watches
                .Where(watch => watch.UserId == userId && watch.IsWishList)
                .MaxAsync(watch => (int?)watch.WishlistPriority, ct) ?? -1) + 1;
            var duplicatesImported = 0;

            await using var transaction = await context.Database.BeginTransactionAsync(ct);
            foreach (var row in selected)
            {
                if (FindDuplicate(row, seen) is not null)
                    duplicatesImported++;

                var watch = new Watch
                {
                    UserId = userId,
                    Brand = row.Watch.Brand,
                    Model = row.Watch.Model,
                    TimesWorn = row.TimesWorn,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                };
                WatchFieldMapper.Apply(watch, row.Watch);
                if (watch.IsWishList)
                    watch.WishlistPriority = wishlistPriority++;
                if (row.Disposition is not null)
                    watch.Disposition = row.Disposition;

                context.Watches.Add(watch);
                seen.Add(new ExistingWatch(0, watch.Brand, watch.Model, watch.Sku, watch.SerialNumber));
            }

            await context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            logger.LogInformation(
                "Imported {WatchCount} watches from Wristcheck for user {UserId}; {SkippedCount} rows skipped",
                selected.Count,
                userId,
                rows.Count - selected.Count);

            return new ExternalImportResultDto
            {
                Imported = selected.Count,
                Skipped = rows.Count - selected.Count,
                DuplicatesImported = duplicatesImported,
            };
        }
        finally
        {
            priorityLock?.Release();
        }
    }

    private static async Task<List<ParsedRow>> ParseAsync(IFormFile file, CancellationToken ct)
    {
        if (file.Length == 0 || !file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Please upload a Wristcheck .csv file.");
        if (file.Length > MaxFileBytes)
            throw new InvalidDataException("CSV files must be 5 MB or smaller.");

        string content;
        using (var reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
            content = await reader.ReadToEndAsync(ct);

        var csvRows = CsvParser.Parse(content);
        if (csvRows.Count < 2)
            throw new InvalidDataException("CSV file is empty or contains only headers.");
        if (csvRows.Count - 1 > MaxRows)
            throw new InvalidDataException($"CSV files may contain at most {MaxRows} watches.");

        var headerEntries = csvRows[0]
            .Select((header, index) => (Header: header.Trim().TrimStart('\uFEFF'), Index: index))
            .ToList();
        var duplicateHeader = headerEntries
            .GroupBy(item => item.Header, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateHeader is not null)
            throw new InvalidDataException($"CSV contains duplicate '{duplicateHeader.Key}' columns.");
        var headers = headerEntries.ToDictionary(
            item => item.Header,
            item => item.Index,
            StringComparer.OrdinalIgnoreCase);
        foreach (var required in new[] { "Status", "Manufacturer", "Model", "Movement", "Tracked Wear Count" })
        {
            if (!headers.ContainsKey(required))
                throw new InvalidDataException($"This is not a supported Wristcheck export: missing '{required}' column.");
        }

        var parsed = new List<ParsedRow>(csvRows.Count - 1);
        for (var index = 1; index < csvRows.Count; index++)
            parsed.Add(ParseRow(index + 1, csvRows[index], headers));
        return parsed;
    }

    private static ParsedRow ParseRow(int rowNumber, string[] values, IReadOnlyDictionary<string, int> headers)
    {
        string Value(string name)
        {
            if (!headers.TryGetValue(name, out var index) || index >= values.Length)
                return "";
            var value = values[index].Trim();
            return value.Equals("null", StringComparison.OrdinalIgnoreCase) ? "" : value;
        }

        var errors = new List<string>();
        var warnings = new List<string>();
        var brand = Value("Manufacturer");
        var model = Value("Model");
        var status = Value("Status");
        var isWishlist = IsWishlistStatus(status);
        var isCollection = IsCollectionStatus(status);
        var isSold = IsSoldStatus(status) || !string.IsNullOrEmpty(Value("Sold Date"));

        if (!isWishlist && !isCollection && !isSold)
            errors.Add($"Unsupported status '{status}'.");

        var dto = new CreateWatchDto
        {
            Brand = brand,
            Model = model,
            MovementType = ParseMovement(Value("Movement"), warnings),
            Category = NullIfEmpty(Value("Category")),
            SerialNumber = NullIfEmpty(Value("Serial Number")),
            Sku = NullIfEmpty(Value("Reference Number")),
            CaseSizeMm = PositiveDouble(Value("Case Diameter"), "Case Diameter", errors),
            CaseThicknessMm = PositiveDouble(Value("Case Thickness"), "Case Thickness", errors),
            CaseMaterial = NullIfEmpty(Value("Case Material")),
            LugWidthMm = PositiveDouble(Value("Lug Width"), "Lug Width", errors),
            LugToLugMm = PositiveDouble(Value("Lug to Lug"), "Lug to Lug", errors),
            WaterResistance = NullIfEmpty(Value("Water Resistance")),
            DateComplication = NullIfEmpty(Value("Date Complication")),
            WarrantyExpiryDate = Date(Value("Warranty Expiry Date"), "Warranty Expiry Date", errors),
            LastServicedDate = Date(Value("Last Serviced Date"), "Last Serviced Date", errors),
            PurchaseDate = Date(Value("Purchase Date"), "Purchase Date", errors),
            PurchasePrice = NonNegativeDecimal(Value("Purchase Price"), "Purchase Price", errors),
            AcquiredFrom = NullIfEmpty(Value("Purchased From")),
            Notes = NullIfEmpty(Value("Notes")),
            WinderTpd = NonNegativeInt(Value("Winder TPD"), "Winder TPD", errors),
            WinderDirection = NullIfEmpty(Value("Winder Direction")),
            IsWishList = isWishlist && !isSold,
        };

        var validationResults = new List<ValidationResult>();
        if (!Validator.TryValidateObject(dto, new ValidationContext(dto), validationResults, validateAllProperties: true))
            errors.AddRange(validationResults.Select(result => result.ErrorMessage ?? "Invalid watch data."));

        var timesWorn = NonNegativeInt(Value("Tracked Wear Count"), "Tracked Wear Count", errors) ?? 0;
        WatchDisposition? disposition = null;
        if (isSold)
        {
            var soldDate = Date(Value("Sold Date"), "Sold Date", errors);
            if (soldDate is null)
                errors.Add("Sold watches require a Sold Date.");
            disposition = new WatchDisposition
            {
                Type = DispositionType.Sold,
                DispositionDate = soldDate ?? DateTime.UtcNow,
                SalePrice = NonNegativeDecimal(Value("Sold Price"), "Sold Price", errors),
                SoldTo = NullIfEmpty(Value("Sold To")),
            };
        }

        return new ParsedRow(rowNumber, dto, disposition, timesWorn, warnings, errors);
    }

    private static ExternalImportRowDto ToPreviewRow(ParsedRow row, Duplicate? duplicate) => new()
    {
        RowNumber = row.RowNumber,
        Brand = row.Watch.Brand,
        Model = row.Watch.Model,
        Destination = row.Disposition is not null ? "Former watch" : row.Watch.IsWishList ? "Wish list" : "Collection",
        CanImport = row.Errors.Count == 0,
        IsDuplicate = duplicate is not null,
        DuplicateWatchId = duplicate?.WatchId is > 0 ? duplicate.WatchId : null,
        DuplicateReason = duplicate?.Reason,
        Warnings = row.Warnings,
        Errors = row.Errors,
    };

    private static Duplicate? FindDuplicate(ParsedRow row, IEnumerable<ExistingWatch> watches)
    {
        var serial = Normalize(row.Watch.SerialNumber);
        var sku = Normalize(row.Watch.Sku);
        var brand = Normalize(row.Watch.Brand);
        var model = Normalize(row.Watch.Model);

        foreach (var watch in watches)
        {
            if (serial.Length > 0 && serial == Normalize(watch.SerialNumber))
                return new Duplicate(watch.Id, "Serial number matches an existing watch.");
        }
        foreach (var watch in watches)
        {
            if (sku.Length > 0 && sku == Normalize(watch.Sku) &&
                brand == Normalize(watch.Brand) && model == Normalize(watch.Model))
                return new Duplicate(watch.Id, "Reference, manufacturer, and model match an existing watch.");
        }
        foreach (var watch in watches)
        {
            if (brand == Normalize(watch.Brand) && model == Normalize(watch.Model))
                return new Duplicate(watch.Id, "Manufacturer and model match an existing watch.");
        }
        return null;
    }

    private static MovementType ParseMovement(string value, List<string> warnings)
    {
        return value.Trim().ToLowerInvariant() switch
        {
            "mechanical - automatic" => MovementType.Automatic,
            "mechanical - manual" => MovementType.Manual,
            "analogue quartz" or "analog quartz" or "solar quartz" => MovementType.Quartz,
            "digital quartz" => MovementType.Digital,
            "" => MovementType.Unknown,
            _ => UnknownMovement(value, warnings),
        };
    }

    private static MovementType UnknownMovement(string value, List<string> warnings)
    {
        warnings.Add($"Movement '{value}' was imported as Unknown.");
        return MovementType.Unknown;
    }

    private static bool IsCollectionStatus(string value) =>
        value.Equals("In Collection", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("Collection", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("Owned", StringComparison.OrdinalIgnoreCase);

    private static bool IsWishlistStatus(string value) =>
        value.Equals("Wish List", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("Wishlist", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("In Wishlist", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("On Wishlist", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("Wanted", StringComparison.OrdinalIgnoreCase);

    private static bool IsSoldStatus(string value) =>
        value.Equals("Sold", StringComparison.OrdinalIgnoreCase);

    private static DateTime? Date(string value, string field, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var parsed))
            return parsed;
        errors.Add($"{field} is not a valid date.");
        return null;
    }

    private static double? PositiveDouble(string value, string field, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value) || value == "0")
            return null;
        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) && parsed > 0)
            return parsed;
        errors.Add($"{field} must be a positive number.");
        return null;
    }

    private static decimal? NonNegativeDecimal(string value, string field, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) && parsed >= 0)
            return parsed;
        errors.Add($"{field} must be a non-negative number.");
        return null;
    }

    private static int? NonNegativeInt(string value, string field, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value) || value == "0")
            return null;
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) && parsed >= 0)
            return parsed;
        errors.Add($"{field} must be a non-negative whole number.");
        return null;
    }

    private static string? NullIfEmpty(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;

    private static string Normalize(string? value) =>
        string.Concat((value ?? "").Where(char.IsLetterOrDigit)).ToLowerInvariant();

    private sealed record ParsedRow(
        int RowNumber,
        CreateWatchDto Watch,
        WatchDisposition? Disposition,
        int TimesWorn,
        List<string> Warnings,
        List<string> Errors);

    private sealed record ExistingWatch(int Id, string Brand, string Model, string? Sku, string? SerialNumber);
    private sealed record Duplicate(int WatchId, string Reason);
}
