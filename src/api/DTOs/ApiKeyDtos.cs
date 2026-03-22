namespace WatchTracker.Api.DTOs;

public class CreateApiKeyDto
{
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
