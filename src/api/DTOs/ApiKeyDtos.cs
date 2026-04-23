using System.ComponentModel.DataAnnotations;

namespace WatchTracker.Api.DTOs;

public class CreateApiKeyDto
{
    [Required, StringLength(100, MinimumLength = 1)]
    public required string Name { get; set; }
}

public class ApiKeyDto
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
}

public class ApiKeyCreatedDto
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Key { get; set; }
    public DateTime CreatedAt { get; set; }
}
