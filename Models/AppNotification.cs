using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatApplicationAPI.Models;

public enum NotificationType
{
    Message = 1,
    FriendRequest = 2,
    FriendAccepted = 3,
    VoiceCall = 4,
    VideoCall = 5,
    MissedCall = 6,
    GroupInvite = 7,
    Story = 8,
    Mention = 9,
    Reaction = 10
}

public class AppNotification
{
    [Key]
    public Guid NotificationId { get; set; } = Guid.NewGuid();

    [Required]
    public Guid UserId { get; set; }
    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    public Guid? SenderId { get; set; }
    public Guid? ChatId { get; set; }
    public Guid? MessageId { get; set; }

    [Required]
    public string Title { get; set; } = string.Empty;
    public string BodyPreview { get; set; } = string.Empty;

    public NotificationType Type { get; set; } = NotificationType.Message;
    public Guid? DeviceId { get; set; }

    public bool IsRead { get; set; } = false;
    public bool IsDelivered { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
