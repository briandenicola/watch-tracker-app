using System.ComponentModel.DataAnnotations;

namespace WatchTracker.Api.DTOs;

public record WatchPatrolStatusDto(bool Connected);
public record WatchPatrolAuthorizationDto(string Url);

public class ConnectWatchPatrolDto
{
    [Required, StringLength(2000)]
    public required string RedirectUri { get; set; }
}

public class CompleteWatchPatrolDto
{
    [Required, StringLength(2000)]
    public required string Code { get; set; }

    [Required, StringLength(200)]
    public required string State { get; set; }
}
