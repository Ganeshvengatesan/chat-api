namespace ChatApplicationAPI.DTOs.Friends;

public class SendFriendRequestDto
{
    public Guid ReceiverId { get; set; }
    public string InitialMessage { get; set; } = string.Empty;
}

public class RespondFriendRequestDto
{
    public Guid RequestId { get; set; }
    public int Action { get; set; } // 2 = Accept, 3 = Reject, 4 = Block
}

public class FriendRequestResponseDto
{
    public Guid RequestId { get; set; }
    public Guid SenderId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string SenderUsername { get; set; } = string.Empty;
    public string SenderAvatarUrl { get; set; } = string.Empty;
    public Guid ReceiverId { get; set; }
    public string InitialMessage { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; }
}
