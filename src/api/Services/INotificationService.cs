using WatchTracker.Api.DTOs;

namespace WatchTracker.Api.Services;

public interface INotificationService
{
    Task<IEnumerable<NotificationDto>> GetAllAsync(int userId, CancellationToken ct = default);
    Task<int> GetUnreadCountAsync(int userId, CancellationToken ct = default);
    Task<NotificationDto?> GetByIdAsync(int id, int userId, CancellationToken ct = default);
    Task<NotificationDto> CreateAsync(CreateNotificationDto dto, int userId, CancellationToken ct = default);
    Task<bool> MarkAsReadAsync(int notificationId, int userId, CancellationToken ct = default);
    Task<bool> MarkAllAsReadAsync(int userId, CancellationToken ct = default);
    Task<bool> DeleteAsync(int notificationId, int userId, CancellationToken ct = default);
}
