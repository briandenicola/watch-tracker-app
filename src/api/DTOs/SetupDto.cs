using System.ComponentModel.DataAnnotations;

namespace WatchTracker.Api.DTOs;

public class SetupDto
{
    [Required, StringLength(100, MinimumLength = 2)]
    public required string Username { get; set; }

    [Required, StringLength(254), EmailAddress]
    public required string Email { get; set; }

    [Required, StringLength(128, MinimumLength = 8)]
    public required string Password { get; set; }

    [StringLength(2000), Url]
    public string? OllamaUrl { get; set; }

    [StringLength(200)]
    public string? OllamaModel { get; set; }

    [StringLength(10000)]
    public string? AiAnalysisPrompt { get; set; }
}
