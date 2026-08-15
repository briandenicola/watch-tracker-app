using System.ComponentModel.DataAnnotations;

namespace WatchTracker.Api.DTOs;

public class ReorderWishlistDto
{
    [Required, MinLength(1)]
    public required List<int> WatchIds { get; set; }
}
