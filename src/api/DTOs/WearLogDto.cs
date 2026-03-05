namespace WatchTracker.Api.DTOs;

public class WearLogDto
{
    public int Id { get; set; }
    public int WatchId { get; set; }
    public string WatchBrand { get; set; } = string.Empty;
    public string WatchModel { get; set; } = string.Empty;
    public DateTime WornDate { get; set; }
}
