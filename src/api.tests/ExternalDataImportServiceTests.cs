using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using WatchTracker.Api.Models;
using WatchTracker.Api.Services;

namespace WatchTracker.Api.Tests;

public class ExternalDataImportServiceTests
{
    [Fact]
    public async Task Preview_detects_wristcheck_and_marks_existing_watch_as_duplicate()
    {
        await using var database = await TestDatabase.CreateAsync();
        var owner = TestDatabase.User("owner");
        var other = TestDatabase.User("other");
        database.Context.AddRange(
            owner,
            other,
            new Watch { User = owner, Brand = "Seiko", Model = "SPB143", Sku = "SPB143" },
            new Watch { User = other, Brand = "Omega", Model = "Seamaster", SerialNumber = "OTHER-1" });
        await database.Context.SaveChangesAsync();
        var service = Service(database);

        var preview = await service.PreviewAsync(owner.Id, File(
            Row("In Collection", "Seiko", "SPB143", "Diver", "", "SPB143", "Mechanical - Automatic"),
            Row("Wish List", "Omega", "Seamaster", "Diver", "OTHER-1", "", "Analogue Quartz")));

        Assert.Equal("Wristcheck", preview.Source);
        Assert.Equal(1, preview.CollectionCount);
        Assert.Equal(1, preview.WishlistCount);
        Assert.Equal(1, preview.DuplicateCount);
        Assert.True(preview.Rows[0].IsDuplicate);
        Assert.Equal("Reference, manufacturer, and model match an existing watch.", preview.Rows[0].DuplicateReason);
        Assert.False(preview.Rows[1].IsDuplicate);
    }

    [Fact]
    public async Task Import_maps_wristcheck_fields_wear_count_wishlist_and_sale()
    {
        await using var database = await TestDatabase.CreateAsync();
        var owner = TestDatabase.User("owner");
        database.Context.Users.Add(owner);
        await database.Context.SaveChangesAsync();
        var service = Service(database);
        var file = File(
            Row(
                "In Collection", "Seiko", "SPB143", "Diver", "SN-1", "SPB143",
                "Mechanical - Automatic", "Date", "2027-02-01", "2026-07-01", "2025-06-01",
                "1200.50", "Dealer", "", "", "", "40.5", "13.2", "Steel", "20", "47.6",
                "200m", "650", "Bidirectional", "\"Daily, favorite\"", "17"),
            Row("Wish List", "Cartier", "Tank", "Dress", "", "", "Mechanical - Manual"),
            Row(
                "Sold", "Casio", "Duro", "Diver", "", "MDV-106", "Digital Quartz",
                "", "", "", "2022-01-01", "65", "Store", "2026-08-01", "50", "Buyer"));

        var result = await service.ImportAsync(owner.Id, file, new HashSet<int> { 2, 3, 4 });

        Assert.Equal(3, result.Imported);
        var watches = await database.Context.Watches
            .Include(watch => watch.Disposition)
            .OrderBy(watch => watch.Id)
            .ToListAsync();
        var seiko = watches[0];
        Assert.Equal(MovementType.Automatic, seiko.MovementType);
        Assert.Equal("Diver", seiko.Category);
        Assert.Equal(40.5, seiko.CaseSizeMm);
        Assert.Equal(13.2, seiko.CaseThicknessMm);
        Assert.Equal("Steel", seiko.CaseMaterial);
        Assert.Equal("Date", seiko.DateComplication);
        Assert.Equal(650, seiko.WinderTpd);
        Assert.Equal("Bidirectional", seiko.WinderDirection);
        Assert.Equal(17, seiko.TimesWorn);
        Assert.Equal("Daily, favorite", seiko.Notes);
        Assert.Equal(new DateTime(2027, 2, 1), seiko.WarrantyExpiryDate);
        Assert.Equal(new DateTime(2026, 7, 1), seiko.LastServicedDate);

        var wishlist = watches[1];
        Assert.True(wishlist.IsWishList);
        Assert.Equal(0, wishlist.WishlistPriority);
        Assert.Equal(MovementType.Manual, wishlist.MovementType);

        var sold = watches[2];
        Assert.False(sold.IsWishList);
        Assert.Equal(MovementType.Digital, sold.MovementType);
        Assert.Equal(DispositionType.Sold, sold.Disposition?.Type);
        Assert.Equal(new DateTime(2026, 8, 1), sold.Disposition?.DispositionDate);
        Assert.Equal(50m, sold.Disposition?.SalePrice);
        Assert.Equal("Buyer", sold.Disposition?.SoldTo);
    }

    [Fact]
    public async Task Import_validates_all_selected_rows_before_writing_any()
    {
        await using var database = await TestDatabase.CreateAsync();
        var owner = TestDatabase.User("owner");
        database.Context.Users.Add(owner);
        await database.Context.SaveChangesAsync();
        var service = Service(database);
        var file = File(
            Row("In Collection", "Seiko", "SPB143", movement: "Mechanical - Automatic"),
            Row("In Collection", "", "No Brand", movement: "Analogue Quartz"));

        var error = await Assert.ThrowsAsync<InvalidDataException>(
            () => service.ImportAsync(owner.Id, file, new HashSet<int> { 2, 3 }));

        Assert.Contains("CSV row 3", error.Message);
        Assert.Empty(database.Context.Watches);
    }

    [Fact]
    public async Task Import_allows_an_explicitly_selected_duplicate_and_reports_it()
    {
        await using var database = await TestDatabase.CreateAsync();
        var owner = TestDatabase.User("owner");
        database.Context.AddRange(
            owner,
            new Watch { User = owner, Brand = "Seiko", Model = "SPB143", Sku = "SPB143" });
        await database.Context.SaveChangesAsync();
        var service = Service(database);
        var file = File(Row(
            "In Collection", "Seiko", "SPB143", reference: "SPB143", movement: "Mechanical - Automatic"));

        var result = await service.ImportAsync(owner.Id, file, new HashSet<int> { 2 });

        Assert.Equal(1, result.Imported);
        Assert.Equal(1, result.DuplicatesImported);
        Assert.Equal(2, await database.Context.Watches.CountAsync());
    }

    [Fact]
    public async Task Preview_reports_unknown_movement_and_status_without_guessing()
    {
        await using var database = await TestDatabase.CreateAsync();
        var owner = TestDatabase.User("owner");
        database.Context.Users.Add(owner);
        await database.Context.SaveChangesAsync();
        var service = Service(database);

        var preview = await service.PreviewAsync(owner.Id, File(
            Row("Somewhere Else", "Brand", "Model", movement: "Spring Drive")));

        var row = Assert.Single(preview.Rows);
        Assert.False(row.CanImport);
        Assert.Contains("Movement 'Spring Drive' was imported as Unknown.", row.Warnings);
        Assert.Contains("Unsupported status 'Somewhere Else'.", row.Errors);
    }

    [Fact]
    public async Task Preview_rejects_a_csv_without_wristcheck_headers()
    {
        await using var database = await TestDatabase.CreateAsync();
        var owner = TestDatabase.User("owner");
        database.Context.Users.Add(owner);
        await database.Context.SaveChangesAsync();
        var service = Service(database);
        var stream = new MemoryStream(Encoding.UTF8.GetBytes("Brand,Model\nSeiko,SPB143"));
        var file = new FormFile(stream, 0, stream.Length, "file", "other.csv");

        var error = await Assert.ThrowsAsync<InvalidDataException>(
            () => service.PreviewAsync(owner.Id, file));

        Assert.Contains("supported Wristcheck export", error.Message);
    }

    private const string Header =
        "Status,Manufacturer,Model,Category,Serial Number,Reference Number,Movement,Date Complication," +
        "Warranty Expiry Date,Last Serviced Date,Purchase Date,Purchase Price,Purchased From,Sold Date," +
        "Sold Price,Sold To,Case Diameter,Case Thickness,Case Material,Lug Width,Lug to Lug,Water Resistance," +
        "Winder TPD,Winder Direction,Notes,Tracked Wear Count";

    private static string Row(
        string status,
        string manufacturer,
        string model,
        string category = "",
        string serial = "",
        string reference = "",
        string movement = "",
        string complication = "",
        string warranty = "",
        string serviced = "",
        string purchased = "",
        string purchasePrice = "",
        string purchasedFrom = "",
        string sold = "",
        string soldPrice = "",
        string soldTo = "",
        string diameter = "",
        string thickness = "",
        string material = "",
        string lugWidth = "",
        string lugToLug = "",
        string waterResistance = "",
        string winderTpd = "",
        string winderDirection = "",
        string notes = "",
        string wearCount = "") =>
        string.Join(',', new[]
        {
            status, manufacturer, model, category, serial, reference, movement, complication, warranty,
            serviced, purchased, purchasePrice, purchasedFrom, sold, soldPrice, soldTo, diameter, thickness,
            material, lugWidth, lugToLug, waterResistance, winderTpd, winderDirection, notes, wearCount,
        });

    private static FormFile File(params string[] rows)
    {
        var content = string.Join('\n', new[] { Header }.Concat(rows));
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        return new FormFile(stream, 0, stream.Length, "file", "wristcheck.csv");
    }

    private static ExternalDataImportService Service(TestDatabase database) =>
        new(database.Context, NullLogger<ExternalDataImportService>.Instance);
}
