namespace WatchTracker.Api.Models;

public class WearLog
{
    public int Id { get; set; }
    public int WatchId { get; set; }
    public Watch Watch { get; set; } = null!;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public DateTime WornDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
