namespace WatchTracker.Api.Models;

public class WatchImage
{
    public int Id { get; set; }
    public int WatchId { get; set; }
    public Watch Watch { get; set; } = null!;
    public required string FileName { get; set; }
    public required string ContentType { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
