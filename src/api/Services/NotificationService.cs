using Microsoft.EntityFrameworkCore;
using WatchTracker.Api.Data;
using WatchTracker.Api.DTOs;
using WatchTracker.Api.Models;

namespace WatchTracker.Api.Services;

public class NotificationService(AppDbContext context) : INotificationService
{
    public async Task<IEnumerable<NotificationDto>> GetAllAsync(int userId, CancellationToken ct = default)
    {
        return await context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => MapToDto(n))
            .ToListAsync(ct);
    }

    public async Task<int> GetUnreadCountAsync(int userId, CancellationToken ct = default)
    {
        return await context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .CountAsync(ct);
    }

    public async Task<NotificationDto?> GetByIdAsync(int id, int userId, CancellationToken ct = default)
    {
        var notification = await context.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId, ct);

        return notification is null ? null : MapToDto(notification);
    }

    public async Task<NotificationDto> CreateAsync(CreateNotificationDto dto, int userId, CancellationToken ct = default)
    {
        var notification = new Notification
        {
            Title = dto.Title,
            Message = dto.Message,
            ActionUrl = dto.ActionUrl,
            UserId = userId
        };

        context.Notifications.Add(notification);
        await context.SaveChangesAsync(ct);

        return MapToDto(notification);
    }

    public async Task<bool> MarkAsReadAsync(int notificationId, int userId, CancellationToken ct = default)
    {
        var notification = await context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId, ct);

        if (notification is null)
            return false;

        notification.IsRead = true;
        await context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> MarkAllAsReadAsync(int userId, CancellationToken ct = default)
    {
        var unreadNotifications = await context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync(ct);

        foreach (var notification in unreadNotifications)
        {
            notification.IsRead = true;
        }

        await context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> DeleteAsync(int notificationId, int userId, CancellationToken ct = default)
    {
        var notification = await context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId, ct);

        if (notification is null)
            return false;

        context.Notifications.Remove(notification);
        await context.SaveChangesAsync(ct);
        return true;
    }

    private static NotificationDto MapToDto(Notification notification) => new()
    {
        Id = notification.Id,
        Title = notification.Title,
        Message = notification.Message,
        ActionUrl = notification.ActionUrl,
        IsRead = notification.IsRead,
        CreatedAt = notification.CreatedAt
    };
}
