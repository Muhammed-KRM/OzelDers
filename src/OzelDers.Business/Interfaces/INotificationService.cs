using OzelDers.Business.DTOs;
using OzelDers.Data.Entities;

namespace OzelDers.Business.Interfaces;

public interface INotificationService
{
    Task<Notification> CreateAsync(Guid userId, string type, string title,
        string message, string? actionUrl = null, string? idempotencyKey = null);
    Task<int> GetUnreadCountAsync(Guid userId);
    Task<List<NotificationDto>> GetUserNotificationsAsync(Guid userId, int page = 1, int pageSize = 20);
    Task MarkAsReadAsync(int notificationId, Guid userId);
    Task MarkAllAsReadAsync(Guid userId);
}
