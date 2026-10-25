namespace WatchTracker.Api.Models;

public enum SavedViewScope
{
    Collection,
    Wishlist
}

public enum LabelMatchMode
{
    Any,
    All
}

/// <summary>
/// A named set of Collection or Wish List filters. Pinned views appear in the navigation.
/// </summary>
public class SavedView
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public SavedViewScope Scope { get; set; }
    public ICollection<Label> Labels { get; set; } = [];
    public LabelMatchMode LabelMatch { get; set; }
    public string? Brand { get; set; }
    public MovementType? MovementType { get; set; }
    public string? SortBy { get; set; }
    public DateTime? PinnedAt { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
