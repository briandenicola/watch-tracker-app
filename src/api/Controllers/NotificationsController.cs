using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WatchTracker.Api.DTOs;
using WatchTracker.Api.Services;

namespace WatchTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController(INotificationService notificationService) : ControllerBase
{
    private int UserId => int.Parse(
        User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<NotificationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<NotificationDto>>> GetAll(CancellationToken ct = default)
    {
        var notifications = await notificationService.GetAllAsync(UserId, ct);
        return Ok(notifications);
    }

    [HttpGet("unread-count")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<ActionResult<int>> GetUnreadCount(CancellationToken ct = default)
    {
        var count = await notificationService.GetUnreadCountAsync(UserId, ct);
        return Ok(count);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(NotificationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NotificationDto>> GetById(int id, CancellationToken ct = default)
    {
        var notification = await notificationService.GetByIdAsync(id, UserId, ct);
        return notification is null ? NotFound() : Ok(notification);
    }

    [HttpPost]
    [ProducesResponseType(typeof(NotificationDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<NotificationDto>> Create(CreateNotificationDto dto, CancellationToken ct = default)
    {
        var notification = await notificationService.CreateAsync(dto, UserId, ct);
        return CreatedAtAction(nameof(GetById), new { id = notification.Id }, notification);
    }

    [HttpPut("{id}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsRead(int id, CancellationToken ct = default)
    {
        var result = await notificationService.MarkAsReadAsync(id, UserId, ct);
        return result ? NoContent() : NotFound();
    }

    [HttpPut("read-all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken ct = default)
    {
        await notificationService.MarkAllAsReadAsync(UserId, ct);
        return NoContent();
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct = default)
    {
        var result = await notificationService.DeleteAsync(id, UserId, ct);
        return result ? NoContent() : NotFound();
    }
}
