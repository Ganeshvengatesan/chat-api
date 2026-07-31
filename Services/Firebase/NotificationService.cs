using ChatApplicationAPI.Data;
using ChatApplicationAPI.DTOs.Notifications;
using ChatApplicationAPI.Interfaces;
using ChatApplicationAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace ChatApplicationAPI.Services.Firebase;

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _db;
    private readonly IFirebaseService _firebaseService;
    private readonly IDeviceTokenService _deviceTokenService;

    public NotificationService(
        ApplicationDbContext db, 
        IFirebaseService firebaseService,
        IDeviceTokenService deviceTokenService)
    {
        _db = db;
        _firebaseService = firebaseService;
        _deviceTokenService = deviceTokenService;
    }

    public async Task<NotificationResponse> CreateAndSendNotificationAsync(NotificationRequest request)
    {
        var notification = new AppNotification
        {
            UserId = request.UserId,
            SenderId = request.SenderId,
            ChatId = request.ChatId,
            MessageId = request.MessageId,
            Title = request.Title,
            BodyPreview = request.Body,
            Type = (NotificationType)request.Type,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        await _db.Notifications.AddAsync(notification);
        await _db.SaveChangesAsync();

        // Dispatch FCM Push Notification to all active devices of target user
        var userDevices = await _deviceTokenService.GetUserActiveDevicesAsync(request.UserId);
        if (userDevices.Any())
        {
            var tokens = userDevices.Select(d => d.DeviceToken).ToList();
            var dataPayload = new Dictionary<string, string>
            {
                { "notificationId", notification.NotificationId.ToString() },
                { "chatId", request.ChatId?.ToString() ?? "" },
                { "type", request.Type.ToString() }
            };

            await _firebaseService.SendMulticastNotificationAsync(tokens, request.Title, request.Body, dataPayload);
            notification.IsDelivered = true;
            await _db.SaveChangesAsync();
        }

        string senderName = string.Empty;
        if (request.SenderId.HasValue)
        {
            var sender = await _db.Users.FindAsync(request.SenderId.Value);
            senderName = sender?.FullName ?? sender?.Username ?? "";
        }

        return new NotificationResponse
        {
            NotificationId = notification.NotificationId,
            UserId = notification.UserId,
            SenderId = notification.SenderId,
            SenderName = senderName,
            ChatId = notification.ChatId,
            Title = notification.Title,
            BodyPreview = notification.BodyPreview,
            Type = (int)notification.Type,
            IsRead = notification.IsRead,
            CreatedAt = notification.CreatedAt
        };
    }

    public async Task<List<NotificationResponse>> GetUserNotificationsAsync(Guid userId)
    {
        return await _db.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new NotificationResponse
            {
                NotificationId = n.NotificationId,
                UserId = n.UserId,
                SenderId = n.SenderId,
                ChatId = n.ChatId,
                Title = n.Title,
                BodyPreview = n.BodyPreview,
                Type = (int)n.Type,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<bool> MarkAsReadAsync(Guid notificationId, Guid userId)
    {
        var notification = await _db.Notifications
            .FirstOrDefaultAsync(n => n.NotificationId == notificationId && n.UserId == userId);

        if (notification != null)
        {
            notification.IsRead = true;
            await _db.SaveChangesAsync();
            return true;
        }
        return false;
    }
}
