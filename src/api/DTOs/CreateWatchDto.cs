using WatchTracker.Api.Models;

namespace WatchTracker.Api.DTOs;

public class CreateWatchDto
{
    public required string Brand { get; set; }
    public required string Model { get; set; }
    public MovementType MovementType { get; set; }
    public double? CaseSizeMm { get; set; }
    public string? BandType { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public decimal? PurchasePrice { get; set; }
    public string? Notes { get; set; }
}
