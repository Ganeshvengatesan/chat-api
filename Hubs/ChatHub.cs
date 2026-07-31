using Microsoft.AspNetCore.SignalR;

namespace ChatApplicationAPI.Hubs;

public class ChatHub : Hub
{
    public async Task JoinChat(string chatId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, chatId);
    }

    public async Task LeaveChat(string chatId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, chatId);
    }

    public async Task SendMessage(string chatId, string senderId, string message, string type)
    {
        await Clients.Group(chatId).SendAsync("ReceiveMessage", new
        {
            chatId,
            senderId,
            message,
            type,
            timestamp = DateTime.UtcNow
        });
    }

    public async Task SendTyping(string chatId, string username, bool isTyping)
    {
        await Clients.OthersInGroup(chatId).SendAsync("UserTyping", new { username, isTyping });
    }

    // ── Voice & Video Call WebRTC Signaling ──

    public async Task InitiateVoiceCall(string targetUserId, string callerName)
    {
        await Clients.User(targetUserId).SendAsync("IncomingVoiceCall", new
        {
            callerId = Context.UserIdentifier ?? Context.ConnectionId,
            callerName,
            callType = "Voice"
        });
    }

    public async Task InitiateVideoCall(string targetUserId, string callerName)
    {
        await Clients.User(targetUserId).SendAsync("IncomingVideoCall", new
        {
            callerId = Context.UserIdentifier ?? Context.ConnectionId,
            callerName,
            callType = "Video"
        });
    }

    public async Task SendOffer(string targetUserId, string sdpOffer)
    {
        await Clients.User(targetUserId).SendAsync("ReceiveOffer", new
        {
            senderId = Context.UserIdentifier ?? Context.ConnectionId,
            sdpOffer
        });
    }

    public async Task SendAnswer(string targetUserId, string sdpAnswer)
    {
        await Clients.User(targetUserId).SendAsync("ReceiveAnswer", new
        {
            senderId = Context.UserIdentifier ?? Context.ConnectionId,
            sdpAnswer
        });
    }

    public async Task SendIceCandidate(string targetUserId, string candidateJson)
    {
        await Clients.User(targetUserId).SendAsync("ReceiveIceCandidate", new
        {
            senderId = Context.UserIdentifier ?? Context.ConnectionId,
            candidateJson
        });
    }

    public async Task RejectCall(string targetUserId, string reason = "User Busy")
    {
        await Clients.User(targetUserId).SendAsync("CallRejected", new
        {
            respondentId = Context.UserIdentifier ?? Context.ConnectionId,
            reason
        });
    }

    public async Task EndCall(string targetUserId)
    {
        await Clients.User(targetUserId).SendAsync("CallEnded", new
        {
            endedBy = Context.UserIdentifier ?? Context.ConnectionId
        });
    }
}
