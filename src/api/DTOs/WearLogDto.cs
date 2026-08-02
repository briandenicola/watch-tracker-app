namespace WatchTracker.Api.DTOs;

public class WearLogDto
{
    public int Id { get; set; }
    public int WatchId { get; set; }
    public string WatchBrand { get; set; } = string.Empty;
    public string WatchModel { get; set; } = string.Empty;
    public DateTime WornDate { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public int? DurationMinutes { get; set; }
    public string? WatchImageUrl { get; set; }
}
