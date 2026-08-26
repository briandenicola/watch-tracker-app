namespace WatchTracker.Api.DTOs;

/// <summary>
/// Optional body for recording a wear. Sent empty (or omitted entirely) the
/// wear is stamped "now", which is what the watch detail page does. The wear
/// log calendar sends a date so a wear can be logged for a past day.
/// </summary>
public class RecordWearDto
{
    public DateTimeOffset? WornDate { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? EndedAt { get; set; }
}
