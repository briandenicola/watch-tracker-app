using System.ComponentModel.DataAnnotations;

namespace WatchTracker.Api.DTOs;

public class ImportImageUrlDto
{
    [Required, StringLength(2000), Url]
    public required string Url { get; set; }
}
