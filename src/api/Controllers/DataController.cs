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
public class DataController(
    AppDbContext context,
    IUploadStorage uploadStorage,
    IDataImportService dataImportService,
    IExternalDataImportService externalDataImportService) : ControllerBase
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
                var imageFileNames = string.Join(";", w.Images.Select(i => Path.GetFileName(i.FileName)));
                var wearDates = string.Join(";", w.WearLogs.OrderByDescending(wl => wl.WornDate).Select(wl => wl.WornDate.ToString("yyyy-MM-dd")));
                var wearLogs = string.Join(";", w.WearLogs.OrderByDescending(wl => wl.WornDate).Select(FormatWearLogExport));

                csv.AppendLine(string.Join(",",
                    w.Id.ToString(CultureInfo.InvariantCulture),
                    Esc(w.Brand),
                    Esc(w.Model),
                    Esc(w.MovementType.ToString()),
                    Esc(w.Category),
                    Esc(w.CaseSizeMm?.ToString(CultureInfo.InvariantCulture)),
                    Esc(w.CaseThicknessMm?.ToString(CultureInfo.InvariantCulture)),
                    Esc(w.CaseMaterial),
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
                    Esc(w.DateComplication),
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
                    Esc(w.WarrantyExpiryDate?.ToString("yyyy-MM-dd")),
                    Esc(w.LastServicedDate?.ToString("yyyy-MM-dd")),
                    Esc(w.WinderTpd?.ToString(CultureInfo.InvariantCulture)),
                    Esc(w.WinderDirection),
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

            // Add images. The archive keeps the bare file name so exports stay
            // portable between accounts, where the owner's directory would not be.
            foreach (var w in watches)
            {
                foreach (var img in w.Images)
                {
                    if (!uploadStorage.TryGetFilePath(img.FileName, out var filePath)) continue;

                    var entry = archive.CreateEntry($"images/{Path.GetFileName(img.FileName)}");
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

    [HttpPost("import/external/preview")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    [ProducesResponseType(typeof(ExternalImportPreviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ExternalImportPreviewDto>> PreviewExternalImport(
        IFormFile file,
        CancellationToken ct)
    {
        try
        {
            return Ok(await externalDataImportService.PreviewAsync(UserId, file, ct));
        }
        catch (InvalidDataException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("import/external")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    [ProducesResponseType(typeof(ExternalImportResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ExternalImportResultDto>> ImportExternal(
        [FromForm] ExternalImportRequestDto request,
        CancellationToken ct)
    {
        var selectedRows = request.SelectedRows
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => int.TryParse(value, CultureInfo.InvariantCulture, out var row) ? row : -1)
            .Where(row => row > 1)
            .ToHashSet();
        try
        {
            return Ok(await externalDataImportService.ImportAsync(UserId, request.File, selectedRows, ct));
        }
        catch (InvalidDataException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    private static readonly string[] CsvColumns =
    [
        "ExportId", "Brand", "Model", "MovementType", "Category", "CaseSizeMm", "CaseThicknessMm", "CaseMaterial", "BandType", "BandColor",
        "PurchaseDate", "PurchasePrice", "AcquisitionType", "AcquiredFrom", "AcquisitionSourceUrl",
        "Notes", "CrystalType", "CaseShape",
        "CrownType", "CalendarType", "DateComplication", "CountryOfOrigin", "WaterResistance",
        "LugWidthMm", "LugToLugMm", "DialColor", "BezelType", "PowerReserveHours", "Sku", "SerialNumber",
        "ProductionYear", "BatteryType", "LastBatteryChangedDate", "WarrantyExpiryDate", "LastServicedDate",
        "WinderTpd", "WinderDirection", "LinkUrl", "LinkText",
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
