namespace WatchTracker.Api.DTOs;

public class UpdateWearLogDateDto
{
    public DateTime WornDate { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
}
