namespace ChatApplicationAPI.DTOs.Notifications;

public class DeviceTokenRequest
{
    public string DeviceToken { get; set; } = string.Empty;
    public string Platform { get; set; } = "Android";
    public string DeviceName { get; set; } = string.Empty;
    public string AppVersion { get; set; } = "1.0.0";
}

public class NotificationRequest
{
    public Guid UserId { get; set; }
    public Guid? SenderId { get; set; }
    public Guid? ChatId { get; set; }
    public Guid? MessageId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public int Type { get; set; } = 1;
}

public class NotificationResponse
{
    public Guid NotificationId { get; set; }
    public Guid UserId { get; set; }
    public Guid? SenderId { get; set; }
    public string? SenderName { get; set; }
    public Guid? ChatId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string BodyPreview { get; set; } = string.Empty;
    public int Type { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}
