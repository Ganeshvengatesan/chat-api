using ChatApplicationAPI.DTOs.Chats;

namespace ChatApplicationAPI.Interfaces;

public interface IChatService
{
    Task<ChatResponseDto> CreateDirectChatAsync(Guid currentUserId, Guid targetUserId);
    Task<ChatResponseDto> CreateGroupChatAsync(Guid ownerUserId, CreateGroupChatDto request);
    Task<List<ChatResponseDto>> GetUserChatsAsync(Guid userId);
    Task<MessageResponseDto> SendMessageAsync(Guid senderId, SendMessageDto request);
    Task<MessageResponseDto> ReactToMessageAsync(Guid userId, Guid messageId, string reaction);
    Task<List<MessageResponseDto>> GetChatMessagesAsync(Guid userId, Guid chatId, int limit = 50);
    Task<bool> AddMemberToGroupAsync(Guid currentUserId, Guid chatId, Guid newUserId, bool makeAdmin = false);
    Task<bool> RemoveMemberFromGroupAsync(Guid currentUserId, Guid chatId, Guid targetUserId);
    Task<bool> DeleteGroupAsync(Guid currentUserId, Guid chatId);
    Task<bool> DeleteMessageAsync(Guid userId, Guid messageId);
}
