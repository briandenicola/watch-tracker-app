namespace WatchTracker.Api.DTOs;

public class UpdateWearLogDateDto
{
    public DateTimeOffset WornDate { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? EndedAt { get; set; }
}
