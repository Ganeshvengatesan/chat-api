using ChatApplicationAPI.Data;
using ChatApplicationAPI.DTOs.Chats;
using ChatApplicationAPI.DTOs.Notifications;
using ChatApplicationAPI.Interfaces;
using ChatApplicationAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace ChatApplicationAPI.Services;

public class ChatService : IChatService
{
    private readonly ApplicationDbContext _db;
    private readonly INotificationService _notificationService;
    private readonly IEncryptionService _encryptionService;

    public ChatService(
        ApplicationDbContext db, 
        INotificationService notificationService,
        IEncryptionService encryptionService)
    {
        _db = db;
        _notificationService = notificationService;
        _encryptionService = encryptionService;
    }

    public async Task<ChatResponseDto> CreateDirectChatAsync(Guid currentUserId, Guid targetUserId)
    {
        var existingChat = await _db.Chats
            .Include(c => c.Participants).ThenInclude(p => p.User)
            .FirstOrDefaultAsync(c => c.Type == ChatType.Direct &&
                c.Participants.Any(p => p.UserId == currentUserId) &&
                c.Participants.Any(p => p.UserId == targetUserId));

        if (existingChat != null)
        {
            return MapChatResponse(existingChat, currentUserId);
        }

        var newChat = new Chat
        {
            Type = ChatType.Direct,
            CreatedBy = currentUserId,
            CreatedAt = DateTime.UtcNow
        };
        await _db.Chats.AddAsync(newChat);
        await _db.SaveChangesAsync();

        var p1 = new ChatParticipant { ChatId = newChat.Id, UserId = currentUserId, Role = ParticipantRole.Member };
        var p2 = new ChatParticipant { ChatId = newChat.Id, UserId = targetUserId, Role = ParticipantRole.Member };
        await _db.ChatParticipants.AddRangeAsync(p1, p2);
        await _db.SaveChangesAsync();

        var chatWithParticipants = await _db.Chats
            .Include(c => c.Participants).ThenInclude(p => p.User)
            .FirstAsync(c => c.Id == newChat.Id);

        return MapChatResponse(chatWithParticipants, currentUserId);
    }

    public async Task<ChatResponseDto> CreateGroupChatAsync(Guid ownerUserId, CreateGroupChatDto request)
    {
        var groupChat = new Chat
        {
            Name = request.Name,
            Description = request.Description,
            Type = ChatType.Group,
            CreatedBy = ownerUserId,
            CreatedAt = DateTime.UtcNow
        };

        await _db.Chats.AddAsync(groupChat);
        await _db.SaveChangesAsync();

        // Owner receives Owner Role
        var ownerParticipant = new ChatParticipant
        {
            ChatId = groupChat.Id,
            UserId = ownerUserId,
            Role = ParticipantRole.Owner
        };
        await _db.ChatParticipants.AddAsync(ownerParticipant);

        // Members receive Member Role
        foreach (var memberId in request.MemberUserIds.Distinct())
        {
            if (memberId != ownerUserId)
            {
                await _db.ChatParticipants.AddAsync(new ChatParticipant
                {
                    ChatId = groupChat.Id,
                    UserId = memberId,
                    Role = ParticipantRole.Member
                });
            }
        }

        await _db.SaveChangesAsync();

        var loadedGroup = await _db.Chats
            .Include(c => c.Participants).ThenInclude(p => p.User)
            .FirstAsync(c => c.Id == groupChat.Id);

        return MapChatResponse(loadedGroup, ownerUserId);
    }

    public async Task<List<ChatResponseDto>> GetUserChatsAsync(Guid userId)
    {
        try
        {
            var chats = await _db.Chats
                .Include(c => c.Participants).ThenInclude(p => p.User)
                .Include(c => c.Messages)
                .Where(c => c.Participants.Any(p => p.UserId == userId))
                .ToListAsync();

            // Ensure Direct Chats exist for all accepted friends
            var acceptedFriendUserIds = await _db.FriendRequests
                .Where(fr => (fr.SenderId == userId || fr.ReceiverId == userId) && fr.Status == RequestStatus.Accepted)
                .Select(fr => fr.SenderId == userId ? fr.ReceiverId : fr.SenderId)
                .ToListAsync();

            if (acceptedFriendUserIds.Any())
            {
                bool chatCreated = false;
                foreach (var friendId in acceptedFriendUserIds)
                {
                    bool chatExists = chats.Any(c => c.Type == ChatType.Direct && c.Participants.Any(p => p.UserId == friendId));
                    if (!chatExists)
                    {
                        var newChat = new Chat
                        {
                            Type = ChatType.Direct,
                            CreatedBy = userId,
                            CreatedAt = DateTime.UtcNow
                        };
                        await _db.Chats.AddAsync(newChat);
                        await _db.SaveChangesAsync();

                        await _db.ChatParticipants.AddRangeAsync(new List<ChatParticipant>
                        {
                            new ChatParticipant { ChatId = newChat.Id, UserId = userId, Role = ParticipantRole.Member },
                            new ChatParticipant { ChatId = newChat.Id, UserId = friendId, Role = ParticipantRole.Member }
                        });
                        await _db.SaveChangesAsync();
                        chatCreated = true;
                    }
                }

                if (chatCreated)
                {
                    chats = await _db.Chats
                        .Include(c => c.Participants).ThenInclude(p => p.User)
                        .Include(c => c.Messages)
                        .Where(c => c.Participants.Any(p => p.UserId == userId))
                        .ToListAsync();
                }
            }

            var pendingSet = new HashSet<Guid>();
            try
            {
                var pendingUserIds = await _db.FriendRequests
                    .Where(fr => (fr.SenderId == userId || fr.ReceiverId == userId) && fr.Status == RequestStatus.Pending)
                    .Select(fr => fr.SenderId == userId ? fr.ReceiverId : fr.SenderId)
                    .ToListAsync();
                pendingSet = new HashSet<Guid>(pendingUserIds);
            }
            catch
            {
                // Fallback gracefully
            }

            var mappedChats = chats.Select(c => MapChatResponse(c, userId, pendingSet)).ToList();

            return mappedChats
                .OrderByDescending(c => c.LastMessageTime ?? DateTime.MinValue)
                .ToList();
        }
        catch
        {
            return new List<ChatResponseDto>();
        }
    }

    public async Task<MessageResponseDto> SendMessageAsync(Guid senderId, SendMessageDto request)
    {
        var isParticipant = await _db.ChatParticipants
            .AnyAsync(cp => cp.ChatId == request.ChatId && cp.UserId == senderId);

        if (!isParticipant)
        {
            throw new UnauthorizedAccessException("You are not a participant in this chat.");
        }

        var encryptedContent = _encryptionService.Encrypt(request.Content);

        var message = new Message
        {
            ChatId = request.ChatId,
            SenderId = senderId,
            Content = encryptedContent,
            MediaUrl = request.MediaUrl,
            Type = (MessageType)request.Type,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        await _db.Messages.AddAsync(message);
        await _db.SaveChangesAsync();

        var sender = await _db.Users.FindAsync(senderId);
        string senderName = sender?.FullName ?? sender?.Username ?? "Someone";

        // Notify other chat participants via Push Notification
        var otherParticipants = await _db.ChatParticipants
            .Where(cp => cp.ChatId == request.ChatId && cp.UserId != senderId)
            .ToListAsync();

        foreach (var participant in otherParticipants)
        {
            await _notificationService.CreateAndSendNotificationAsync(new NotificationRequest
            {
                UserId = participant.UserId,
                SenderId = senderId,
                ChatId = request.ChatId,
                MessageId = message.Id,
                Title = senderName,
                Body = request.Content,
                Type = (int)NotificationType.Message
            });
        }

        return new MessageResponseDto
        {
            MessageId = message.Id,
            ChatId = message.ChatId,
            SenderId = message.SenderId,
            SenderName = senderName,
            Content = request.Content, // Return original plain text to sender
            MediaUrl = message.MediaUrl,
            Reaction = message.Reaction,
            Type = (int)message.Type,
            IsRead = message.IsRead,
            CreatedAt = message.CreatedAt
        };
    }

    public async Task<MessageResponseDto> ReactToMessageAsync(Guid userId, Guid messageId, string reaction)
    {
        var message = await _db.Messages
            .Include(m => m.Sender)
            .FirstOrDefaultAsync(m => m.Id == messageId);

        if (message == null)
        {
            throw new ArgumentException("Message not found.");
        }

        var isParticipant = await _db.ChatParticipants
            .AnyAsync(cp => cp.ChatId == message.ChatId && cp.UserId == userId);

        if (!isParticipant)
        {
            throw new UnauthorizedAccessException("Access denied.");
        }

        message.Reaction = reaction;
        await _db.SaveChangesAsync();

        return new MessageResponseDto
        {
            MessageId = message.Id,
            ChatId = message.ChatId,
            SenderId = message.SenderId,
            SenderName = message.Sender?.FullName ?? "",
            Content = _encryptionService.Decrypt(message.Content),
            MediaUrl = message.MediaUrl,
            Reaction = message.Reaction,
            Type = (int)message.Type,
            IsRead = message.IsRead,
            CreatedAt = message.CreatedAt
        };
    }

    public async Task<List<MessageResponseDto>> GetChatMessagesAsync(Guid userId, Guid chatId, int limit = 50)
    {
        try
        {
            var isParticipant = await _db.ChatParticipants
                .AnyAsync(cp => cp.ChatId == chatId && cp.UserId == userId);

            if (!isParticipant)
            {
                throw new UnauthorizedAccessException("Access denied.");
            }

            var rawMessages = await _db.Messages
                .Include(m => m.Sender)
                .Where(m => m.ChatId == chatId)
                .OrderByDescending(m => m.CreatedAt)
                .Take(limit)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();

            // Mark all unread messages received by this user as read
            var unreadMessages = rawMessages
                .Where(m => m.SenderId != userId && !m.IsRead)
                .ToList();

            if (unreadMessages.Any())
            {
                foreach (var unreadMsg in unreadMessages)
                {
                    unreadMsg.IsRead = true;
                }
                try
                {
                    await _db.SaveChangesAsync();
                }
                catch {}
            }

            return rawMessages.Select(m => new MessageResponseDto
            {
                MessageId = m.Id,
                ChatId = m.ChatId,
                SenderId = m.SenderId,
                SenderName = m.Sender != null ? (m.Sender.FullName ?? m.Sender.Username) : "",
                Content = SafeDecrypt(m.Content),
                MediaUrl = m.MediaUrl ?? "",
                Reaction = m.Reaction ?? "",
                Type = (int)m.Type,
                IsRead = m.IsRead,
                CreatedAt = m.CreatedAt
            }).ToList();
        }
        catch
        {
            return new List<MessageResponseDto>();
        }
    }

    private string SafeDecrypt(string content)
    {
        if (string.IsNullOrEmpty(content)) return "";
        try
        {
            return _encryptionService.Decrypt(content);
        }
        catch
        {
            return content;
        }
    }

    public async Task<bool> AddMemberToGroupAsync(Guid currentUserId, Guid chatId, Guid newUserId, bool makeAdmin = false)
    {
        var currentRole = await GetParticipantRoleAsync(chatId, currentUserId);
        if (currentRole != ParticipantRole.Owner && currentRole != ParticipantRole.Admin)
        {
            throw new UnauthorizedAccessException("Only Group Owners and Admins can add new members.");
        }

        var existing = await _db.ChatParticipants
            .FirstOrDefaultAsync(cp => cp.ChatId == chatId && cp.UserId == newUserId);

        if (existing != null) return false;

        var newParticipant = new ChatParticipant
        {
            ChatId = chatId,
            UserId = newUserId,
            Role = makeAdmin ? ParticipantRole.Admin : ParticipantRole.Member
        };
        await _db.ChatParticipants.AddAsync(newParticipant);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RemoveMemberFromGroupAsync(Guid currentUserId, Guid chatId, Guid targetUserId)
    {
        var currentRole = await GetParticipantRoleAsync(chatId, currentUserId);
        var targetRole = await GetParticipantRoleAsync(chatId, targetUserId);

        // Owners can remove anyone. Admins can remove Members, but NOT Owners or other Admins.
        if (currentRole == ParticipantRole.Owner || (currentRole == ParticipantRole.Admin && targetRole == ParticipantRole.Member))
        {
            var participant = await _db.ChatParticipants
                .FirstOrDefaultAsync(cp => cp.ChatId == chatId && cp.UserId == targetUserId);

            if (participant != null)
            {
                _db.ChatParticipants.Remove(participant);
                await _db.SaveChangesAsync();
                return true;
            }
        }
        else
        {
            throw new UnauthorizedAccessException("You do not have permission to remove this participant.");
        }

        return false;
    }

    public async Task<bool> DeleteGroupAsync(Guid currentUserId, Guid chatId)
    {
        var currentRole = await GetParticipantRoleAsync(chatId, currentUserId);
        if (currentRole != ParticipantRole.Owner)
        {
            throw new UnauthorizedAccessException("Only the Group Owner can delete this group.");
        }

        var chat = await _db.Chats.FindAsync(chatId);
        if (chat != null)
        {
            _db.Chats.Remove(chat);
            await _db.SaveChangesAsync();
            return true;
        }

        return false;
    }

    private async Task<ParticipantRole?> GetParticipantRoleAsync(Guid chatId, Guid userId)
    {
        var p = await _db.ChatParticipants
            .FirstOrDefaultAsync(cp => cp.ChatId == chatId && cp.UserId == userId);
        return p?.Role;
    }

    private ChatResponseDto MapChatResponse(Chat c, Guid currentUserId, HashSet<Guid>? pendingSet = null)
    {
        var participants = c.Participants ?? new List<ChatParticipant>();
        var messages = c.Messages ?? new List<Message>();

        var userParticipant = participants.FirstOrDefault(p => p.UserId == currentUserId);
        var otherParticipant = participants.FirstOrDefault(p => p.UserId != currentUserId);

        string chatName = c.Name ?? "Chat";
        string iconUrl = c.GroupIconUrl ?? "";

        if (c.Type == ChatType.Direct && otherParticipant?.User != null)
        {
            chatName = string.IsNullOrEmpty(otherParticipant.User.FullName) 
                ? (otherParticipant.User.Username ?? "User") 
                : otherParticipant.User.FullName;
            iconUrl = otherParticipant.User.AvatarUrl ?? "";
        }

        bool isPending = false;
        if (c.Type == ChatType.Direct && otherParticipant != null && pendingSet != null)
        {
            isPending = pendingSet.Contains(otherParticipant.UserId);
        }

        var lastMessage = messages.OrderByDescending(m => m.CreatedAt).FirstOrDefault();

        string decryptedLastMsg = "";
        if (lastMessage != null && !string.IsNullOrEmpty(lastMessage.Content))
        {
            try
            {
                decryptedLastMsg = _encryptionService.Decrypt(lastMessage.Content);
            }
            catch
            {
                decryptedLastMsg = lastMessage.Content;
            }
        }

        return new ChatResponseDto
        {
            ChatId = c.Id,
            Name = chatName,
            Type = c.Type.ToString(),
            IconUrl = iconUrl,
            LastMessage = decryptedLastMsg,
            LastMessageTime = lastMessage?.CreatedAt ?? c.CreatedAt,
            UnreadCount = messages.Count(m => !m.IsRead && m.SenderId != currentUserId),
            UserRole = userParticipant?.Role.ToString() ?? "Member",
            IsPendingRequest = isPending,
            Participants = participants.Select(p => new ParticipantDto
            {
                UserId = p.UserId,
                Username = p.User?.Username ?? "",
                FullName = p.User?.FullName ?? "",
                AvatarUrl = p.User?.AvatarUrl ?? "",
                Role = p.Role.ToString(),
                IsOnline = p.User?.IsOnline ?? false
            }).ToList()
        };
    }

    public async Task<bool> DeleteMessageAsync(Guid userId, Guid messageId)
    {
        var message = await _db.Messages.FirstOrDefaultAsync(m => m.Id == messageId);
        if (message == null) return false;

        var isParticipant = await _db.ChatParticipants.AnyAsync(cp => cp.ChatId == message.ChatId && cp.UserId == userId);
        if (!isParticipant)
        {
            throw new UnauthorizedAccessException("You are not a participant in this chat.");
        }

        _db.Messages.Remove(message);
        await _db.SaveChangesAsync();
        return true;
    }
}
