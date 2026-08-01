namespace ChatApplicationAPI.DTOs.Chats;

public class CreateDirectChatDto
{
    public Guid TargetUserId { get; set; }
}

public class CreateGroupChatDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<Guid> MemberUserIds { get; set; } = new List<Guid>();
}

public class SendMessageDto
{
    public Guid ChatId { get; set; }
    public string Content { get; set; } = string.Empty;
    public int Type { get; set; } = 1; // 1 = Text, 2 = Image, 3 = Voice
    public string MediaUrl { get; set; } = string.Empty;
}

public class ChatResponseDto
{
    public Guid ChatId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "Direct";
    public string IconUrl { get; set; } = string.Empty;
    public string LastMessage { get; set; } = string.Empty;
    public DateTime? LastMessageTime { get; set; }
    public int UnreadCount { get; set; }
    public string UserRole { get; set; } = "Member";
    public bool IsPendingRequest { get; set; }
    public List<ParticipantDto> Participants { get; set; } = new List<ParticipantDto>();
}

public class ParticipantDto
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public string Role { get; set; } = "Member";
    public bool IsOnline { get; set; }
}

public class ReactMessageDto
{
    public string Reaction { get; set; } = string.Empty;
}

public class MessageResponseDto
{
    public Guid MessageId { get; set; }
    public Guid ChatId { get; set; }
    public Guid SenderId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string MediaUrl { get; set; } = string.Empty;
    public string Reaction { get; set; } = string.Empty;
    public int Type { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}
