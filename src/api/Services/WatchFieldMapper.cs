using WatchTracker.Api.DTOs;
using WatchTracker.Api.Models;

namespace WatchTracker.Api.Services;

internal static class WatchFieldMapper
{
    public static void Apply(Watch watch, WatchFieldsDto dto)
    {
        watch.Brand = dto.Brand;
        watch.Model = dto.Model;
        watch.MovementType = dto.MovementType;
        watch.CaseSizeMm = dto.CaseSizeMm;
        watch.BandType = dto.BandType;
        watch.BandColor = dto.BandColor;
        watch.PurchaseDate = dto.PurchaseDate;
        watch.PurchasePrice = dto.PurchasePrice;
        watch.AcquisitionType = dto.AcquisitionType;
        watch.AcquiredFrom = dto.AcquiredFrom;
        watch.AcquisitionSourceUrl = dto.AcquisitionSourceUrl;
        watch.Notes = dto.Notes;
        watch.CrystalType = dto.CrystalType;
        watch.CaseShape = dto.CaseShape;
        watch.CrownType = dto.CrownType;
        watch.CalendarType = dto.CalendarType;
        watch.CountryOfOrigin = dto.CountryOfOrigin;
        watch.WaterResistance = dto.WaterResistance;
        watch.LugWidthMm = dto.LugWidthMm;
        watch.LugToLugMm = dto.LugToLugMm;
        watch.DialColor = dto.DialColor;
        watch.BezelType = dto.BezelType;
        watch.PowerReserveHours = dto.PowerReserveHours;
        watch.Sku = dto.Sku;
        watch.SerialNumber = dto.SerialNumber;
        watch.ProductionYear = dto.ProductionYear;
        watch.BatteryType = dto.BatteryType;
        watch.LastBatteryChangedDate = dto.LastBatteryChangedDate;
        watch.LinkUrl = dto.LinkUrl;
        watch.LinkText = dto.LinkText;
        watch.StorageLocation = dto.StorageLocation;
        watch.IsWishList = dto.IsWishList;
    }
}
