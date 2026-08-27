using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO.Compression;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using WatchTracker.Api.Data;
using WatchTracker.Api.DTOs;
using WatchTracker.Api.Models;

namespace WatchTracker.Api.Services;

public class DataImportService(AppDbContext context, IWebHostEnvironment env) : IDataImportService
{
    public async Task<DataImportOutcome> ImportAsync(
        int userId,
        IFormFile file,
        CancellationToken ct = default)
    {
        if (file.Length == 0 || !file.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            return DataImportOutcome.Failure("Please upload a .zip file.");

        var uploadsDir = Path.Combine(env.ContentRootPath, "uploads");
        Directory.CreateDirectory(uploadsDir);

        int watchesImported = 0;
        int imagesImported = 0;
        int wearLogsImported = 0;
        var imageRenames = new Dictionary<string, string>();

        using var stream = file.OpenReadStream();
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

        // Find CSV
        var csvEntry = archive.Entries.FirstOrDefault(e =>
            e.FullName.Equals("watches.csv", StringComparison.OrdinalIgnoreCase));
        if (csvEntry is null)
            return DataImportOutcome.Failure("ZIP must contain watches.csv at the root level.");

        // Read CSV
        List<string[]> rows;
        using (var reader = new StreamReader(csvEntry.Open(), Encoding.UTF8))
        {
            var content = await reader.ReadToEndAsync();
            rows = ParseCsv(content);
        }

        if (rows.Count < 2)
            return DataImportOutcome.Failure("CSV file is empty or contains only headers.");

        var headers = rows[0];
        var colMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < headers.Length; i++)
            colMap[headers[i].Trim()] = i;

        // Extract all images from ZIP into uploads dir first
        var imageEntries = archive.Entries
            .Where(e => e.FullName.StartsWith("images/", StringComparison.OrdinalIgnoreCase) && e.Length > 0)
            .ToList();

        foreach (var imgEntry in imageEntries)
        {
            var destFileName = Path.GetFileName(imgEntry.FullName);
            if (string.IsNullOrEmpty(destFileName)) continue;

            // Use a new GUID to avoid collisions and prevent path traversal
            var ext = Path.GetExtension(destFileName).ToLowerInvariant();
            if (ext is not (".jpg" or ".jpeg" or ".png" or ".webp" or ".gif"))
                continue;

            var newFileName = $"{Guid.NewGuid()}{ext}";
            var destPath = Path.Combine(uploadsDir, newFileName);

            // Verify the resolved path is still within uploads directory
            if (!Path.GetFullPath(destPath).StartsWith(Path.GetFullPath(uploadsDir), StringComparison.OrdinalIgnoreCase))
                continue;

            await using var entryStream = imgEntry.Open();
            await using var fileStream = new FileStream(destPath, FileMode.Create);
            await entryStream.CopyToAsync(fileStream);

            // Track the rename so we can map old -> new
            imageRenames[destFileName] = newFileName;
            imagesImported++;
        }

        // Import watches
        var importedWatchIds = new Dictionary<int, int>();
        var pendingTradeLinks = new List<(WatchDisposition Disposition, int ExportWatchId)>();
        var wishlistPriorityBase = (await context.Watches
            .Where(w => w.UserId == userId && w.IsWishList)
            .MaxAsync(w => (int?)w.WishlistPriority) ?? -1) + 1;
        var nextLegacyWishlistPriority = wishlistPriorityBase + rows.Count;
        for (int r = 1; r < rows.Count; r++)
        {
            var row = rows[r];
            string Val(string col) => colMap.TryGetValue(col, out var idx) && idx < row.Length ? row[idx] : "";

            var isWishList = Val("IsWishList").Equals("true", StringComparison.OrdinalIgnoreCase);
            var importedWishlistPriority = int.TryParse(Val("WishlistPriority"), out var wishlistPriority)
                ? wishlistPriorityBase + wishlistPriority
                : nextLegacyWishlistPriority++;

            var watch = new Watch
            {
                UserId = userId,
                Brand = Val("Brand"),
                Model = Val("Model"),
                MovementType = Enum.TryParse<MovementType>(Val("MovementType"), true, out var mt) ? mt : MovementType.Automatic,
                CaseSizeMm = double.TryParse(Val("CaseSizeMm"), CultureInfo.InvariantCulture, out var cs) ? cs : null,
                BandType = NullIfEmpty(Val("BandType")),
                BandColor = NullIfEmpty(Val("BandColor")),
                PurchaseDate = DateTime.TryParse(Val("PurchaseDate"), CultureInfo.InvariantCulture, DateTimeStyles.None, out var pd) ? pd : null,
                PurchasePrice = decimal.TryParse(Val("PurchasePrice"), CultureInfo.InvariantCulture, out var pp) ? pp : null,
                AcquisitionType = Enum.TryParse<AcquisitionType>(Val("AcquisitionType"), true, out var at)
                    ? at
                    : AcquisitionType.New,
                AcquiredFrom = NullIfEmpty(Val("AcquiredFrom")),
                AcquisitionSourceUrl = NullIfEmpty(Val("AcquisitionSourceUrl")),
                Notes = NullIfEmpty(Val("Notes")),
                CrystalType = NullIfEmpty(Val("CrystalType")),
                CaseShape = NullIfEmpty(Val("CaseShape")),
                CrownType = NullIfEmpty(Val("CrownType")),
                CalendarType = NullIfEmpty(Val("CalendarType")),
                CountryOfOrigin = NullIfEmpty(Val("CountryOfOrigin")),
                WaterResistance = NullIfEmpty(Val("WaterResistance")),
                LugWidthMm = double.TryParse(Val("LugWidthMm"), CultureInfo.InvariantCulture, out var lw) ? lw : null,
                LugToLugMm = double.TryParse(Val("LugToLugMm"), CultureInfo.InvariantCulture, out var l2l) ? l2l : null,
                DialColor = NullIfEmpty(Val("DialColor")),
                BezelType = NullIfEmpty(Val("BezelType")),
                PowerReserveHours = int.TryParse(Val("PowerReserveHours"), out var pr) ? pr : null,
                Sku = NullIfEmpty(Val("Sku")),
                SerialNumber = NullIfEmpty(Val("SerialNumber")),
                ProductionYear = int.TryParse(Val("ProductionYear"), out var py) ? py : null,
                BatteryType = NullIfEmpty(Val("BatteryType")),
                LastBatteryChangedDate = DateTime.TryParse(Val("LastBatteryChangedDate"), CultureInfo.InvariantCulture, DateTimeStyles.None, out var lbcd) ? lbcd : null,
                LinkUrl = NullIfEmpty(Val("LinkUrl")),
                LinkText = NullIfEmpty(Val("LinkText")),
                StorageLocation = NullIfEmpty(Val("StorageLocation")),
                IsWishList = isWishList,
                WishlistPriority = isWishList ? importedWishlistPriority : null,
                TimesWorn = int.TryParse(Val("TimesWorn"), out var tw) ? tw : 0,
                LastWornDate = DateTime.TryParse(Val("LastWornDate"), CultureInfo.InvariantCulture, DateTimeStyles.None, out var lwd) ? lwd : null,
                CreatedAt = DateTime.TryParse(Val("CreatedAt"), CultureInfo.InvariantCulture, DateTimeStyles.None, out var ca) ? ca : DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            var hasDisposition = Enum.TryParse<DispositionType>(
                Val("DispositionType"),
                true,
                out var dispositionType);
            if (!hasDisposition && Val("IsRetired").Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                dispositionType = DispositionType.Retired;
                hasDisposition = true;
            }

            if (hasDisposition)
            {
                var dispositionDateValue = Val("DispositionDate");
                if (string.IsNullOrWhiteSpace(dispositionDateValue))
                    dispositionDateValue = Val("RetiredAt");

                var receivedWatchName = NullIfEmpty(Val("TradeReceivedWatch"));
                var tradeDetails = NullIfEmpty(Val("TradeDetails"));
                if (tradeDetails is null)
                    tradeDetails = receivedWatchName;

                var importedDisposition = new UpdateWatchDispositionDto
                {
                    Type = dispositionType,
                    DispositionDate = DateTime.TryParse(
                        dispositionDateValue,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out var dispositionDate)
                            ? dispositionDate
                            : watch.UpdatedAt,
                    Notes = NullIfEmpty(Val("DispositionNotes")),
                    SoldTo = NullIfEmpty(Val("SoldTo")),
                    SalePrice = decimal.TryParse(Val("SalePrice"), CultureInfo.InvariantCulture, out var salePrice)
                        ? salePrice
                        : null,
                    ReceivedWatchId = int.TryParse(
                        Val("TradeReceivedWatchExportId"),
                        CultureInfo.InvariantCulture,
                        out var receivedWatchExportId)
                            ? receivedWatchExportId
                            : null,
                    TradeDetails = tradeDetails,
                    OtherLabel = NullIfEmpty(Val("OtherLabel")),
                    ReturnReason = NullIfEmpty(Val("ReturnReason")),
                    ReturnedTo = NullIfEmpty(Val("ReturnedTo")),
                    RefundAmount = decimal.TryParse(Val("RefundAmount"), CultureInfo.InvariantCulture, out var refundAmount)
                        ? refundAmount
                        : null,
                };

                var validationResults = new List<ValidationResult>();
                if (!Validator.TryValidateObject(
                    importedDisposition,
                    new ValidationContext(importedDisposition),
                    validationResults,
                    validateAllProperties: true))
                {
                    return DataImportOutcome.Failure(
                        $"Watch CSV row {r + 1}: {validationResults[0].ErrorMessage}");
                }

                watch.Disposition = new WatchDisposition
                {
                    Type = importedDisposition.Type,
                    DispositionDate = importedDisposition.DispositionDate,
                    Notes = importedDisposition.Notes,
                    SoldTo = importedDisposition.SoldTo,
                    SalePrice = importedDisposition.SalePrice,
                    TradeDetails = importedDisposition.TradeDetails,
                    OtherLabel = importedDisposition.OtherLabel,
                    ReturnReason = importedDisposition.ReturnReason,
                    ReturnedTo = importedDisposition.ReturnedTo,
                    RefundAmount = importedDisposition.RefundAmount,
                };
            }

            context.Watches.Add(watch);
            await context.SaveChangesAsync();

            if (int.TryParse(Val("ExportId"), CultureInfo.InvariantCulture, out var exportId))
                importedWatchIds[exportId] = watch.Id;

            if (watch.Disposition is not null
                && int.TryParse(
                    Val("TradeReceivedWatchExportId"),
                    CultureInfo.InvariantCulture,
                    out var receivedWatchExportIdToResolve))
            {
                pendingTradeLinks.Add((watch.Disposition, receivedWatchExportIdToResolve));
            }

            // Link images
            var imageFileNames = Val("Images").Split(';', StringSplitOptions.RemoveEmptyEntries);
            int sortOrder = 0;
            foreach (var origFileName in imageFileNames)
            {
                var trimmed = origFileName.Trim();
                if (!imageRenames.TryGetValue(trimmed, out var newFileName)) continue;

                var contentType = InferContentType(newFileName);
                context.WatchImages.Add(new WatchImage
                {
                    WatchId = watch.Id,
                    FileName = newFileName,
                    ContentType = contentType,
                    SortOrder = sortOrder++,
                });
            }

            // Import wear logs. WearLogs preserves start/end times; WearDates keeps older exports compatible.
            var wearLogEntries = Val("WearLogs").Split(';', StringSplitOptions.RemoveEmptyEntries);
            if (wearLogEntries.Length > 0)
            {
                foreach (var entry in wearLogEntries)
                {
                    var parsed = ParseWearLogExport(entry);
                    if (parsed is null) continue;

                    context.WearLogs.Add(new WearLog
                    {
                        WatchId = watch.Id,
                        UserId = userId,
                        WornDate = parsed.Value.WornDate,
                        StartedAt = parsed.Value.StartedAt,
                        EndedAt = parsed.Value.EndedAt,
                    });
                    wearLogsImported++;
                }
            }
            else
            {
                var wearDates = Val("WearDates").Split(';', StringSplitOptions.RemoveEmptyEntries);
                foreach (var dateStr in wearDates)
                {
                    if (DateTime.TryParse(dateStr.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var wornDate))
                    {
                        context.WearLogs.Add(new WearLog
                        {
                            WatchId = watch.Id,
                        UserId = userId,
                            WornDate = wornDate,
                        });
                        wearLogsImported++;
                    }
                }
            }

            await context.SaveChangesAsync();
            watchesImported++;
        }

        foreach (var (disposition, exportWatchId) in pendingTradeLinks)
        {
            if (importedWatchIds.TryGetValue(exportWatchId, out var receivedWatchId)
                && receivedWatchId != disposition.WatchId)
            {
                disposition.ReceivedWatchId = receivedWatchId;
            }
        }
        await context.SaveChangesAsync();

        return DataImportOutcome.Success(new ImportResultDto
        {
            WatchesImported = watchesImported,
            ImagesImported = imagesImported,
            WearLogsImported = wearLogsImported,
        });
    }

    private static string? NullIfEmpty(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;

    private static string InferContentType(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".gif" => "image/gif",
            _ => "image/jpeg",
        };
    }

    private static (DateTime WornDate, DateTime? StartedAt, DateTime? EndedAt)? ParseWearLogExport(string value)
    {
        var parts = value.Split('|');
        if (parts.Length == 0 ||
            !DateTime.TryParse(parts[0], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var wornDate))
        {
            return null;
        }

        DateTime? startedAt = parts.Length > 1 &&
            DateTime.TryParse(parts[1], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsedStart)
                ? parsedStart
                : null;
        DateTime? endedAt = parts.Length > 2 &&
            DateTime.TryParse(parts[2], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsedEnd)
                ? parsedEnd
                : null;

        return (wornDate, startedAt, endedAt);
    }

    /// <summary>Simple RFC 4180 CSV parser that handles quoted fields.</summary>
    private static List<string[]> ParseCsv(string content)
    {
        var rows = new List<string[]>();
        var fields = new List<string>();
        var field = new StringBuilder();
        bool inQuotes = false;
        int i = 0;

        while (i < content.Length)
        {
            char c = content[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < content.Length && content[i + 1] == '"')
                    {
                        field.Append('"');
                        i += 2;
                    }
                    else
                    {
                        inQuotes = false;
                        i++;
                    }
                }
                else
                {
                    field.Append(c);
                    i++;
                }
            }
            else
            {
                if (c == '"')
                {
                    inQuotes = true;
                    i++;
                }
                else if (c == ',')
                {
                    fields.Add(field.ToString());
                    field.Clear();
                    i++;
                }
                else if (c == '\r' || c == '\n')
                {
                    fields.Add(field.ToString());
                    field.Clear();
                    if (fields.Any(f => f.Length > 0))
                        rows.Add(fields.ToArray());
                    fields.Clear();
                    if (c == '\r' && i + 1 < content.Length && content[i + 1] == '\n')
                        i++;
                    i++;
                }
                else
                {
                    field.Append(c);
                    i++;
                }
            }
        }

        // Last row
        if (field.Length > 0 || fields.Count > 0)
        {
            fields.Add(field.ToString());
            if (fields.Any(f => f.Length > 0))
                rows.Add(fields.ToArray());
        }

        return rows;
    }
}
