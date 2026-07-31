using ChatApplicationAPI.DTOs.Friends;

namespace ChatApplicationAPI.Interfaces;

public interface IFriendService
{
    Task<FriendRequestResponseDto> SendFriendOrMessageRequestAsync(Guid senderId, SendFriendRequestDto request);
    Task<bool> RespondToRequestAsync(Guid receiverId, RespondFriendRequestDto request);
    Task<List<FriendRequestResponseDto>> GetPendingRequestsAsync(Guid userId);
    Task<List<UserSearchResultDto>> SearchUsersAsync(Guid currentUserId, string query);
}

public class UserSearchResultDto
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public string FriendshipStatus { get; set; } = "None"; // None, Pending, Accepted, Blocked
}
