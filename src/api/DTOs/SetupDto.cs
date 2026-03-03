namespace WatchTracker.Api.DTOs;

public class SetupDto
{
    public required string Username { get; set; }
    public required string Email { get; set; }
    public required string Password { get; set; }
    public string? AnthropicApiKey { get; set; }
    public string? AiAnalysisPrompt { get; set; }
}
