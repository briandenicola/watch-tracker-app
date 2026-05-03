using System.ComponentModel.DataAnnotations;

namespace WatchTracker.Api.DTOs;

public class NotificationDto
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public required string Message { get; set; }
    public string? ActionUrl { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateNotificationDto
{
    [Required, StringLength(200)]
    public required string Title { get; set; }

    [Required, StringLength(1000)]
    public required string Message { get; set; }

    [StringLength(500), Url]
    public string? ActionUrl { get; set; }
}

public class MarkNotificationReadDto
{
    [Required]
    public required int NotificationId { get; set; }
}
