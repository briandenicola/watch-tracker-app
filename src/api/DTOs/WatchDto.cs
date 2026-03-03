using WatchTracker.Api.Models;

namespace WatchTracker.Api.DTOs;

public class WatchDto
{
    public int Id { get; set; }
    public required string Brand { get; set; }
    public required string Model { get; set; }
    public MovementType MovementType { get; set; }
    public double? CaseSizeMm { get; set; }
    public string? BandType { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public decimal? PurchasePrice { get; set; }
    public string? Notes { get; set; }
    public string? AiAnalysis { get; set; }
    public DateTime? LastWornDate { get; set; }
    public int TimesWorn { get; set; }
    public List<WatchImageDto> ImageUrls { get; set; } = [];
    public string BrandName => Brand;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
