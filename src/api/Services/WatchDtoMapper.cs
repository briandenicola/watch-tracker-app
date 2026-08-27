using WatchTracker.Api.DTOs;
using WatchTracker.Api.Models;

namespace WatchTracker.Api.Services;

internal static class WatchDtoMapper
{
    public static WatchDto Map(Watch watch) => new()
    {
        Id = watch.Id,
        Brand = watch.Brand,
        Model = watch.Model,
        MovementType = watch.MovementType,
        CaseSizeMm = watch.CaseSizeMm,
        BandType = watch.BandType,
        BandColor = watch.BandColor,
        PurchaseDate = watch.PurchaseDate,
        PurchasePrice = watch.PurchasePrice,
        AcquisitionType = watch.AcquisitionType,
        AcquiredFrom = watch.AcquiredFrom,
        AcquisitionSourceUrl = watch.AcquisitionSourceUrl,
        Notes = watch.Notes,
        AiAnalysis = watch.AiAnalysis,
        LastWornDate = watch.LastWornDate,
        TimesWorn = watch.TimesWorn,
        CurrentResaleValue = watch.CurrentResaleValue,
        ResaleValueUpdatedAt = watch.ResaleValueUpdatedAt,
        CrystalType = watch.CrystalType,
        CaseShape = watch.CaseShape,
        CrownType = watch.CrownType,
        CalendarType = watch.CalendarType,
        CountryOfOrigin = watch.CountryOfOrigin,
        WaterResistance = watch.WaterResistance,
        LugWidthMm = watch.LugWidthMm,
        LugToLugMm = watch.LugToLugMm,
        DialColor = watch.DialColor,
        BezelType = watch.BezelType,
        PowerReserveHours = watch.PowerReserveHours,
        Sku = watch.Sku,
        SerialNumber = watch.SerialNumber,
        ProductionYear = watch.ProductionYear,
        BatteryType = watch.BatteryType,
        LastBatteryChangedDate = watch.LastBatteryChangedDate,
        LinkUrl = watch.LinkUrl,
        LinkText = watch.LinkText,
        MarketplaceCurrency = watch.MarketplaceCurrency,
        MarketplaceObservedAt = watch.MarketplaceObservedAt,
        StorageLocation = watch.StorageLocation,
        IsWishList = watch.IsWishList,
        WishlistPriority = watch.WishlistPriority,
        Disposition = watch.Disposition is null ? null : new WatchDispositionDto
        {
            Type = watch.Disposition.Type,
            DispositionDate = watch.Disposition.DispositionDate,
            Notes = watch.Disposition.Notes,
            SoldTo = watch.Disposition.SoldTo,
            SalePrice = watch.Disposition.SalePrice,
            ReceivedWatchId = watch.Disposition.ReceivedWatchId,
            ReceivedWatchName = watch.Disposition.ReceivedWatch is null
                ? null
                : $"{watch.Disposition.ReceivedWatch.Brand} {watch.Disposition.ReceivedWatch.Model}",
            TradeDetails = watch.Disposition.TradeDetails,
            OtherLabel = watch.Disposition.OtherLabel,
            ReturnReason = watch.Disposition.ReturnReason,
            ReturnedTo = watch.Disposition.ReturnedTo,
            RefundAmount = watch.Disposition.RefundAmount,
        },
        ImageUrls = watch.Images.OrderBy(i => i.SortOrder).Select(i => new WatchImageDto
        {
            Id = i.Id,
            Url = $"/uploads/{i.FileName}"
        }).ToList(),
        CreatedAt = watch.CreatedAt,
        UpdatedAt = watch.UpdatedAt
    };
}
