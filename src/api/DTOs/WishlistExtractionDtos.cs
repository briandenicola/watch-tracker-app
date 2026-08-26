using System.ComponentModel.DataAnnotations;

namespace WatchTracker.Api.DTOs;

public class WishlistExtractionRequestDto
{
    [Required, StringLength(2000), Url]
    public required string Url { get; set; }
}

public class WishlistExtractionResultDto
{
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public decimal? PurchasePrice { get; set; }
    public required string LinkUrl { get; set; }
    public required string LinkText { get; set; }
    public string? ImageUrl { get; set; }
    public List<string> Warnings { get; set; } = [];
}
