namespace WatchTracker.Api.Models;

public class PriceAlert
{
    public int Id { get; set; }
    public int PriceObservationId { get; set; }
    public PriceObservation PriceObservation { get; set; } = null!;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public PriceAlertTrigger Trigger { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
