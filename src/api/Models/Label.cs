namespace WatchTracker.Api.Models;

public class Label
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string Color { get; set; } = "#6b7280";
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public ICollection<Watch> Watches { get; set; } = [];
    public ICollection<SavedView> SavedViews { get; set; } = [];
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
