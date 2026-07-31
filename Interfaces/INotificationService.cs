using ChatApplicationAPI.DTOs.Notifications;
using ChatApplicationAPI.Models;

namespace ChatApplicationAPI.Interfaces;

public interface INotificationService
{
    Task<NotificationResponse> CreateAndSendNotificationAsync(NotificationRequest request);
    Task<List<NotificationResponse>> GetUserNotificationsAsync(Guid userId);
    Task<bool> MarkAsReadAsync(Guid notificationId, Guid userId);
}
