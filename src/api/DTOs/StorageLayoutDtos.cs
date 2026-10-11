using System.ComponentModel.DataAnnotations;

namespace WatchTracker.Api.DTOs;

public class StorageLayoutDto
{
    [Range(1, StorageLayoutLimits.MaxDimension)]
    public int Rows { get; set; }

    [Range(1, StorageLayoutLimits.MaxDimension)]
    public int Columns { get; set; }
}

public static class StorageLayoutLimits
{
    public const int MaxDimension = 12;
}

public class StorageSlotPlacementDto
{
    public int WatchId { get; set; }

    [Range(0, StorageLayoutLimits.MaxDimension * StorageLayoutLimits.MaxDimension - 1)]
    public int Slot { get; set; }
}

public class UpdateStorageSlotsDto
{
    [Required, StringLength(100, MinimumLength = 1)]
    public required string Location { get; set; }

    [Required, MaxLength(StorageLayoutLimits.MaxDimension * StorageLayoutLimits.MaxDimension)]
    public required List<StorageSlotPlacementDto> Placements { get; set; }
}
