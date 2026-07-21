namespace WatchTracker.Api.Models;

public class ResaleValueEntry
{
    public int Id { get; set; }
    public int WatchId { get; set; }
    public Watch Watch { get; set; } = null!;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public decimal Value { get; set; }
    public ResaleValueSource Source { get; set; }
    public string? Reasoning { get; set; }
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
