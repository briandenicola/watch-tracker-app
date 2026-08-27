using System.Globalization;
using System.IO.Compression;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WatchTracker.Api.Data;
using WatchTracker.Api.DTOs;
using WatchTracker.Api.Models;
using WatchTracker.Api.Services;

namespace WatchTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DataController(AppDbContext context, IWebHostEnvironment env, IDataImportService dataImportService) : ControllerBase
{
    private int UserId => int.Parse(
        User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("export")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Export()
    {
        var watches = await context.Watches
            .Include(w => w.Images.OrderBy(i => i.SortOrder))
            .Include(w => w.WearLogs)
            .Include(w => w.Disposition)
                .ThenInclude(d => d!.ReceivedWatch)
            .Where(w => w.UserId == UserId)
            .OrderBy(w => w.Id)
            .ToListAsync();

        var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            // Build CSV
            var csv = new StringBuilder();
            csv.AppendLine(string.Join(",", CsvColumns));

            foreach (var w in watches)
            {
                var imageFileNames = string.Join(";", w.Images.Select(i => i.FileName));
                var wearDates = string.Join(";", w.WearLogs.OrderByDescending(wl => wl.WornDate).Select(wl => wl.WornDate.ToString("yyyy-MM-dd")));
                var wearLogs = string.Join(";", w.WearLogs.OrderByDescending(wl => wl.WornDate).Select(FormatWearLogExport));

                csv.AppendLine(string.Join(",",
                    w.Id.ToString(CultureInfo.InvariantCulture),
                    Esc(w.Brand),
                    Esc(w.Model),
                    Esc(w.MovementType.ToString()),
                    Esc(w.CaseSizeMm?.ToString(CultureInfo.InvariantCulture)),
                    Esc(w.BandType),
                    Esc(w.BandColor),
                    Esc(w.PurchaseDate?.ToString("yyyy-MM-dd")),
                    Esc(w.PurchasePrice?.ToString(CultureInfo.InvariantCulture)),
                    Esc(w.AcquisitionType.ToString()),
                    Esc(w.AcquiredFrom),
                    Esc(w.AcquisitionSourceUrl),
                    Esc(w.Notes),
                    Esc(w.CrystalType),
                    Esc(w.CaseShape),
                    Esc(w.CrownType),
                    Esc(w.CalendarType),
                    Esc(w.CountryOfOrigin),
                    Esc(w.WaterResistance),
                    Esc(w.LugWidthMm?.ToString(CultureInfo.InvariantCulture)),
                    Esc(w.LugToLugMm?.ToString(CultureInfo.InvariantCulture)),
                    Esc(w.DialColor),
                    Esc(w.BezelType),
                    Esc(w.PowerReserveHours?.ToString()),
                    Esc(w.Sku),
                    Esc(w.SerialNumber),
                    Esc(w.ProductionYear?.ToString(CultureInfo.InvariantCulture)),
                    Esc(w.BatteryType),
                    Esc(w.LastBatteryChangedDate?.ToString("yyyy-MM-dd")),
                    Esc(w.LinkUrl),
                    Esc(w.LinkText),
                    Esc(w.StorageLocation),
                    w.IsWishList ? "true" : "false",
                    Esc(w.WishlistPriority?.ToString(CultureInfo.InvariantCulture)),
                    Esc(w.Disposition?.Type.ToString()),
                    Esc(w.Disposition?.DispositionDate.ToString("yyyy-MM-dd")),
                    Esc(w.Disposition?.Notes),
                    Esc(w.Disposition?.SoldTo),
                    Esc(w.Disposition?.SalePrice?.ToString(CultureInfo.InvariantCulture)),
                    Esc(w.Disposition?.ReceivedWatchId?.ToString(CultureInfo.InvariantCulture)),
                    Esc(w.Disposition?.ReceivedWatch is null
                        ? null
                        : $"{w.Disposition.ReceivedWatch.Brand} {w.Disposition.ReceivedWatch.Model}"),
                    Esc(w.Disposition?.TradeDetails),
                    Esc(w.Disposition?.OtherLabel),
                    Esc(w.Disposition?.ReturnReason),
                    Esc(w.Disposition?.ReturnedTo),
                    Esc(w.Disposition?.RefundAmount?.ToString(CultureInfo.InvariantCulture)),
                    w.TimesWorn.ToString(),
                    Esc(w.LastWornDate?.ToString("yyyy-MM-dd")),
                    Esc(w.CreatedAt.ToString("yyyy-MM-dd")),
                    Esc(imageFileNames),
                    Esc(wearDates),
                    Esc(wearLogs)
                ));
            }

            var csvEntry = archive.CreateEntry("watches.csv");
            await using (var writer = new StreamWriter(csvEntry.Open(), Encoding.UTF8))
            {
                await writer.WriteAsync(csv.ToString());
            }

            // Add images
            var uploadsDir = Path.Combine(env.ContentRootPath, "uploads");
            foreach (var w in watches)
            {
                foreach (var img in w.Images)
                {
                    var filePath = Path.Combine(uploadsDir, img.FileName);
                    if (!System.IO.File.Exists(filePath)) continue;

                    var entry = archive.CreateEntry($"images/{img.FileName}");
                    await using var entryStream = entry.Open();
                    await using var fileStream = System.IO.File.OpenRead(filePath);
                    await fileStream.CopyToAsync(entryStream);
                }
            }
        }

        memoryStream.Position = 0;
        return File(memoryStream, "application/zip", "watch-collection-export.zip");
    }

    [HttpPost("import")]
    [ProducesResponseType(typeof(ImportResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ImportResultDto>> Import(IFormFile file, CancellationToken ct)
    {
        var outcome = await dataImportService.ImportAsync(UserId, file, ct);
        return outcome.Result is null
            ? BadRequest(new { error = outcome.Error })
            : Ok(outcome.Result);
    }

    private static readonly string[] CsvColumns =
    [
        "ExportId", "Brand", "Model", "MovementType", "CaseSizeMm", "BandType", "BandColor",
        "PurchaseDate", "PurchasePrice", "AcquisitionType", "AcquiredFrom", "AcquisitionSourceUrl",
        "Notes", "CrystalType", "CaseShape",
        "CrownType", "CalendarType", "CountryOfOrigin", "WaterResistance",
        "LugWidthMm", "LugToLugMm", "DialColor", "BezelType", "PowerReserveHours", "Sku", "SerialNumber",
        "ProductionYear", "BatteryType", "LastBatteryChangedDate", "LinkUrl", "LinkText",
        "StorageLocation", "IsWishList", "WishlistPriority", "DispositionType", "DispositionDate", "DispositionNotes",
        "SoldTo", "SalePrice", "TradeReceivedWatchExportId", "TradeReceivedWatch", "TradeDetails", "OtherLabel", "ReturnReason",
        "ReturnedTo", "RefundAmount", "TimesWorn", "LastWornDate", "CreatedAt", "Images",
        "WearDates", "WearLogs"
    ];

    private static string Esc(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }

    private static string FormatWearLogExport(WearLog log) =>
        string.Join("|",
            log.WornDate.ToString("O", CultureInfo.InvariantCulture),
            log.StartedAt?.ToString("O", CultureInfo.InvariantCulture) ?? "",
            log.EndedAt?.ToString("O", CultureInfo.InvariantCulture) ?? "");
}
