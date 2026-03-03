namespace WatchTracker.Api.Models;

public class Watch
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
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public ICollection<WatchImage> Images { get; set; } = [];
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
