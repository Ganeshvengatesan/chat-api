using ChatApplicationAPI.Data;
using ChatApplicationAPI.DTOs.Friends;
using ChatApplicationAPI.DTOs.Notifications;
using ChatApplicationAPI.Interfaces;
using ChatApplicationAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace ChatApplicationAPI.Services;

public class FriendService : IFriendService
{
    private readonly ApplicationDbContext _db;
    private readonly INotificationService _notificationService;

    public FriendService(ApplicationDbContext db, INotificationService notificationService)
    {
        _db = db;
        _notificationService = notificationService;
    }

    public async Task<FriendRequestResponseDto> SendFriendOrMessageRequestAsync(Guid senderId, SendFriendRequestDto request)
    {
        var existingRequest = await _db.FriendRequests
            .FirstOrDefaultAsync(fr => 
                (fr.SenderId == senderId && fr.ReceiverId == request.ReceiverId) ||
                (fr.SenderId == request.ReceiverId && fr.ReceiverId == senderId));

        if (existingRequest != null)
        {
            if (existingRequest.Status == RequestStatus.Blocked)
            {
                throw new InvalidOperationException("Cannot send message or friend request to this user.");
            }
            if (existingRequest.Status == RequestStatus.Accepted)
            {
                throw new InvalidOperationException("You are already friends with this user.");
            }
        }

        var friendRequest = new FriendRequest
        {
            SenderId = senderId,
            ReceiverId = request.ReceiverId,
            InitialMessage = request.InitialMessage,
            Status = RequestStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _db.FriendRequests.AddAsync(friendRequest);
        await _db.SaveChangesAsync();

        // Also ensure a Direct Chat container exists
        var existingChat = await _db.Chats
            .Include(c => c.Participants)
            .FirstOrDefaultAsync(c => c.Type == ChatType.Direct &&
                c.Participants.Any(p => p.UserId == senderId) &&
                c.Participants.Any(p => p.UserId == request.ReceiverId));

        if (existingChat == null)
        {
            var newChat = new Chat
            {
                Type = ChatType.Direct,
                CreatedBy = senderId,
                CreatedAt = DateTime.UtcNow
            };
            await _db.Chats.AddAsync(newChat);
            await _db.SaveChangesAsync();

            await _db.ChatParticipants.AddRangeAsync(new List<ChatParticipant>
            {
                new ChatParticipant { ChatId = newChat.Id, UserId = senderId, Role = ParticipantRole.Member },
                new ChatParticipant { ChatId = newChat.Id, UserId = request.ReceiverId, Role = ParticipantRole.Member }
            });
            await _db.SaveChangesAsync();
        }

        // Send Push Notification for Friend/Message Request
        var sender = await _db.Users.FindAsync(senderId);
        string senderName = sender?.FullName ?? sender?.Username ?? "Someone";

        await _notificationService.CreateAndSendNotificationAsync(new NotificationRequest
        {
            UserId = request.ReceiverId,
            SenderId = senderId,
            Title = "New Message Request",
            Body = $"{senderName} wants to message you: \"{request.InitialMessage}\"",
            Type = (int)NotificationType.FriendRequest
        });

        return new FriendRequestResponseDto
        {
            RequestId = friendRequest.RequestId,
            SenderId = friendRequest.SenderId,
            SenderName = senderName,
            SenderUsername = sender?.Username ?? "",
            SenderAvatarUrl = sender?.AvatarUrl ?? "",
            ReceiverId = friendRequest.ReceiverId,
            InitialMessage = friendRequest.InitialMessage,
            Status = friendRequest.Status.ToString(),
            CreatedAt = friendRequest.CreatedAt
        };
    }

    public async Task<bool> RespondToRequestAsync(Guid currentUserId, RespondFriendRequestDto request)
    {
        // 1. Try finding by RequestId matching Sender or Receiver
        var friendRequest = await _db.FriendRequests
            .FirstOrDefaultAsync(fr => fr.RequestId == request.RequestId && 
                (fr.ReceiverId == currentUserId || fr.SenderId == currentUserId));

        // 2. If not found by RequestId, check if request.RequestId is actually a ChatId
        if (friendRequest == null)
        {
            var chatParticipants = await _db.ChatParticipants
                .Where(cp => cp.ChatId == request.RequestId)
                .Select(cp => cp.UserId)
                .ToListAsync();

            if (chatParticipants.Contains(currentUserId))
            {
                var otherUserId = chatParticipants.FirstOrDefault(id => id != currentUserId);
                if (otherUserId != Guid.Empty)
                {
                    friendRequest = await _db.FriendRequests
                        .FirstOrDefaultAsync(fr => 
                            (fr.SenderId == currentUserId && fr.ReceiverId == otherUserId) ||
                            (fr.SenderId == otherUserId && fr.ReceiverId == currentUserId));
                }
            }
        }

        // 3. Fallback: find any pending request involving currentUserId
        if (friendRequest == null)
        {
            friendRequest = await _db.FriendRequests
                .FirstOrDefaultAsync(fr => (fr.ReceiverId == currentUserId || fr.SenderId == currentUserId) && fr.Status == RequestStatus.Pending);
        }

        if (friendRequest == null) return false;

        var newStatus = (RequestStatus)request.Action;
        friendRequest.Status = newStatus;
        friendRequest.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        if (newStatus == RequestStatus.Accepted)
        {
            var user = await _db.Users.FindAsync(currentUserId);
            string name = user?.FullName ?? user?.Username ?? "Someone";

            var targetUserId = friendRequest.SenderId == currentUserId ? friendRequest.ReceiverId : friendRequest.SenderId;

            await _notificationService.CreateAndSendNotificationAsync(new NotificationRequest
            {
                UserId = targetUserId,
                SenderId = currentUserId,
                Title = "Friend Request Accepted",
                Body = $"{name} accepted your message request!",
                Type = (int)NotificationType.FriendAccepted
            });
        }

        return true;
    }

    public async Task<List<FriendRequestResponseDto>> GetPendingRequestsAsync(Guid userId)
    {
        return await _db.FriendRequests
            .Include(fr => fr.Sender)
            .Where(fr => fr.ReceiverId == userId && fr.Status == RequestStatus.Pending)
            .OrderByDescending(fr => fr.CreatedAt)
            .Select(fr => new FriendRequestResponseDto
            {
                RequestId = fr.RequestId,
                SenderId = fr.SenderId,
                SenderName = fr.Sender != null ? fr.Sender.FullName : "",
                SenderUsername = fr.Sender != null ? fr.Sender.Username : "",
                SenderAvatarUrl = fr.Sender != null ? fr.Sender.AvatarUrl : "",
                ReceiverId = fr.ReceiverId,
                InitialMessage = fr.InitialMessage,
                Status = fr.Status.ToString(),
                CreatedAt = fr.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<List<UserSearchResultDto>> GetFriendsAsync(Guid userId)
    {
        // 1. Get user IDs of accepted friend requests
        var friendUserIds = await _db.FriendRequests
            .Where(fr => (fr.SenderId == userId || fr.ReceiverId == userId) && fr.Status == RequestStatus.Accepted)
            .Select(fr => fr.SenderId == userId ? fr.ReceiverId : fr.SenderId)
            .ToListAsync();

        // 2. Get user IDs from direct chats
        var directChatUserIds = await _db.Chats
            .Include(c => c.Participants)
            .Where(c => c.Type == ChatType.Direct && c.Participants.Any(p => p.UserId == userId))
            .SelectMany(c => c.Participants)
            .Where(p => p.UserId != userId)
            .Select(p => p.UserId)
            .ToListAsync();

        var allFriendIds = friendUserIds.Concat(directChatUserIds).Distinct().ToList();

        if (!allFriendIds.Any())
        {
            return new List<UserSearchResultDto>();
        }

        var friends = await _db.Users
            .Where(u => allFriendIds.Contains(u.Id))
            .Select(u => new UserSearchResultDto
            {
                UserId = u.Id,
                Username = u.Username,
                FullName = u.FullName,
                AvatarUrl = u.AvatarUrl ?? "",
                FriendshipStatus = "Accepted"
            })
            .ToListAsync();

        return friends;
    }

    public async Task<List<UserSearchResultDto>> SearchUsersAsync(Guid currentUserId, string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return new List<UserSearchResultDto>();

        var queryLower = query.ToLower();
        var users = await _db.Users
            .Where(u => u.Id != currentUserId && 
                (u.Username.ToLower().Contains(queryLower) || u.FullName.ToLower().Contains(queryLower)))
            .Take(20)
            .ToListAsync();

        var requests = await _db.FriendRequests
            .Where(fr => (fr.SenderId == currentUserId || fr.ReceiverId == currentUserId))
            .ToListAsync();

        var results = new List<UserSearchResultDto>();
        foreach (var u in users)
        {
            var req = requests.FirstOrDefault(r => 
                (r.SenderId == currentUserId && r.ReceiverId == u.Id) ||
                (r.SenderId == u.Id && r.ReceiverId == currentUserId));

            string statusStr = "None";
            if (req != null)
            {
                statusStr = req.Status.ToString();
            }

            results.Add(new UserSearchResultDto
            {
                UserId = u.Id,
                Username = u.Username,
                FullName = u.FullName,
                AvatarUrl = u.AvatarUrl,
                FriendshipStatus = statusStr
            });
        }

        return results;
    }
}
