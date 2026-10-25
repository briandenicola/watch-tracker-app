using System.ComponentModel.DataAnnotations;
using WatchTracker.Api.Models;

namespace WatchTracker.Api.DTOs;

public class LabelDto
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Color { get; set; }
    public int WatchCount { get; set; }
}

public class SaveLabelDto
{
    [Required, StringLength(50, MinimumLength = 1)]
    public required string Name { get; set; }

    [RegularExpression("^#[0-9a-fA-F]{6}$", ErrorMessage = "Color must be a hex value like #c4a35a.")]
    public string? Color { get; set; }
}

public class SetWatchLabelsDto
{
    public List<int> LabelIds { get; set; } = [];
}

public class SavedViewDto
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public SavedViewScope Scope { get; set; }
    public List<int> LabelIds { get; set; } = [];
    public LabelMatchMode LabelMatch { get; set; }
    public string? Brand { get; set; }
    public MovementType? MovementType { get; set; }
    public string? SortBy { get; set; }
    public bool Pinned { get; set; }
    public DateTime? PinnedAt { get; set; }
}

public class SaveSavedViewDto
{
    [Required, StringLength(80, MinimumLength = 1)]
    public required string Name { get; set; }
    public SavedViewScope Scope { get; set; }
    public List<int> LabelIds { get; set; } = [];
    public LabelMatchMode LabelMatch { get; set; }

    [StringLength(100)]
    public string? Brand { get; set; }
    public MovementType? MovementType { get; set; }

    [StringLength(20)]
    public string? SortBy { get; set; }
    public bool Pinned { get; set; }
}

public class PinSavedViewDto
{
    public bool Pinned { get; set; }
}
