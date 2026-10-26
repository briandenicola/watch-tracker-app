using Microsoft.EntityFrameworkCore;
using WatchTracker.Api.Data;
using WatchTracker.Api.DTOs;
using WatchTracker.Api.Models;

namespace WatchTracker.Api.Services;

/// <summary>
/// The read-only feed the household life store reads in full on each poll.
/// It lists every watch the user owns, disposed and wish list items included,
/// with its children embedded. It changes nothing.
/// </summary>
public class LifestoreRecordsService(AppDbContext context) : ILifestoreRecordsService
{
    public const int DefaultLimit = 100;
    public const int MaxLimit = 500;
    private const string TokenPrefix = "watch:";

    public async Task<LifestoreRecordsPageDto> PageAsync(int userId, string? token, int limit, CancellationToken ct = default)
    {
        var after = ParseToken(token);
        limit = limit <= 0 ? DefaultLimit : Math.Min(limit, MaxLimit);

        // One extra row says whether another page follows.
        var watches = await context.Watches
            .AsNoTracking()
            .AsSplitQuery()
            .Include(w => w.Labels)
            .Include(w => w.WearLogs)
            .Include(w => w.ResaleValueEntries)
            .Include(w => w.Disposition)
            .Where(w => w.UserId == userId && w.Id > after)
            .OrderBy(w => w.Id)
            .Take(limit + 1)
            .ToListAsync(ct);

        var page = new LifestoreRecordsPageDto();
        if (watches.Count > limit)
        {
            watches = watches[..limit];
            page.Next = TokenPrefix + watches[^1].Id;
        }
        page.Records = watches.Select(Map).ToList();
        return page;
    }

    private static int ParseToken(string? token)
    {
        if (string.IsNullOrEmpty(token)) return 0;
        if (token.StartsWith(TokenPrefix, StringComparison.Ordinal)
            && int.TryParse(token[TokenPrefix.Length..], System.Globalization.NumberStyles.None, null, out var id)
            && id > 0)
            return id;
        throw new ArgumentException("The page token is not one this feed gave out. Start again without a page.");
    }

    private static LifestoreRecordDto Map(Watch w)
    {
        var wears = w.WearLogs.OrderBy(l => l.WornDate).ThenBy(l => l.Id).ToList();
        var values = w.ResaleValueEntries.OrderBy(v => v.RecordedAt).ThenBy(v => v.Id).ToList();
        var updated = new[] { w.UpdatedAt }
            .Concat(wears.Select(l => l.CreatedAt))
            .Concat(values.Select(v => v.CreatedAt))
            .Max();

        return new LifestoreRecordDto
        {
            Id = w.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
            UpdatedAt = updated,
            Data = new LifestoreWatchDto
            {
                Id = w.Id,
                Brand = w.Brand,
                Model = w.Model,
                MovementType = w.MovementType,
                Category = w.Category,
                CaseSizeMm = w.CaseSizeMm,
                CaseThicknessMm = w.CaseThicknessMm,
                CaseMaterial = w.CaseMaterial,
                BandType = w.BandType,
                BandColor = w.BandColor,
                DialColor = w.DialColor,
                CrystalType = w.CrystalType,
                CaseShape = w.CaseShape,
                CrownType = w.CrownType,
                CalendarType = w.CalendarType,
                DateComplication = w.DateComplication,
                BezelType = w.BezelType,
                WaterResistance = w.WaterResistance,
                LugWidthMm = w.LugWidthMm,
                LugToLugMm = w.LugToLugMm,
                PowerReserveHours = w.PowerReserveHours,
                CountryOfOrigin = w.CountryOfOrigin,
                Sku = w.Sku,
                SerialNumber = w.SerialNumber,
                ProductionYear = w.ProductionYear,
                BatteryType = w.BatteryType,
                PurchaseDate = w.PurchaseDate,
                PurchasePrice = w.PurchasePrice,
                AcquisitionType = w.AcquisitionType,
                AcquiredFrom = w.AcquiredFrom,
                AcquisitionSourceUrl = w.AcquisitionSourceUrl,
                Notes = w.Notes,
                CurrentResaleValue = w.CurrentResaleValue,
                LastBatteryChangedDate = w.LastBatteryChangedDate,
                WarrantyExpiryDate = w.WarrantyExpiryDate,
                LastServicedDate = w.LastServicedDate,
                StorageLocation = w.StorageLocation,
                StorageSlot = w.StorageSlot,
                IsWishList = w.IsWishList,
                WishlistPriority = w.WishlistPriority,
                LinkUrl = w.LinkUrl,
                TimesWorn = w.TimesWorn,
                LastWornDate = w.LastWornDate,
                CreatedAt = w.CreatedAt,
                UpdatedAt = w.UpdatedAt,
                Labels = w.Labels.Select(l => l.Name).Order(StringComparer.Ordinal).ToList(),
                Wears = wears.Select(l => new LifestoreWearDto { Id = l.Id, WornDate = l.WornDate, StartedAt = l.StartedAt, EndedAt = l.EndedAt }).ToList(),
                ResaleValues = values.Select(v => new LifestoreResaleValueDto { Id = v.Id, Value = v.Value, Source = v.Source, RecordedAt = v.RecordedAt }).ToList(),
                Disposition = w.Disposition is not { } d ? null : new LifestoreDispositionDto
                {
                    Type = d.Type,
                    DispositionDate = d.DispositionDate,
                    Notes = d.Notes,
                    SoldTo = d.SoldTo,
                    SalePrice = d.SalePrice,
                    ReceivedWatchId = d.ReceivedWatchId,
                    TradeDetails = d.TradeDetails,
                    OtherLabel = d.OtherLabel,
                    ReturnReason = d.ReturnReason,
                    ReturnedTo = d.ReturnedTo,
                    RefundAmount = d.RefundAmount,
                },
            },
        };
    }
}
