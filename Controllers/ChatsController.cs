using ChatApplicationAPI.DTOs.Chats;
using ChatApplicationAPI.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ChatApplicationAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ChatsController : ControllerBase
{
    private readonly IChatService _chatService;

    public ChatsController(IChatService chatService)
    {
        _chatService = chatService;
    }

    [HttpGet]
    public async Task<IActionResult> GetUserChats()
    {
        var userId = GetCurrentUserId();
        var chats = await _chatService.GetUserChatsAsync(userId);
        return Ok(chats);
    }

    [HttpPost("direct")]
    public async Task<IActionResult> CreateDirectChat([FromBody] CreateDirectChatDto request)
    {
        var userId = GetCurrentUserId();
        var chat = await _chatService.CreateDirectChatAsync(userId, request.TargetUserId);
        return Ok(chat);
    }

    [HttpPost("group")]
    public async Task<IActionResult> CreateGroupChat([FromBody] CreateGroupChatDto request)
    {
        var userId = GetCurrentUserId();
        var chat = await _chatService.CreateGroupChatAsync(userId, request);
        return Ok(chat);
    }

    [HttpPost("messages")]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageDto request)
    {
        var userId = GetCurrentUserId();
        try
        {
            var msg = await _chatService.SendMessageAsync(userId, request);
            return Ok(msg);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadMedia([FromForm] IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "No file provided." });
        }

        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        var fileExt = Path.GetExtension(file.FileName);
        var fileName = $"{Guid.NewGuid()}{fileExt}";
        var filePath = Path.Combine(uploadsFolder, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var relativeUrl = $"/uploads/{fileName}";
        var sizeInMb = (file.Length / (1024.0 * 1024.0)).ToString("0.0");

        return Ok(new
        {
            url = relativeUrl,
            fileName = file.FileName,
            fileSize = $"{sizeInMb} MB"
        });
    }

    [HttpGet("{chatId:guid}/messages")]
    public async Task<IActionResult> GetMessages(Guid chatId, [FromQuery] int limit = 50)
    {
        var userId = GetCurrentUserId();
        try
        {
            var messages = await _chatService.GetChatMessagesAsync(userId, chatId, limit);
            return Ok(messages);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
    }

    [HttpPost("{chatId:guid}/members")]
    public async Task<IActionResult> AddMember(Guid chatId, [FromQuery] Guid targetUserId, [FromQuery] bool makeAdmin = false)
    {
        var userId = GetCurrentUserId();
        try
        {
            var success = await _chatService.AddMemberToGroupAsync(userId, chatId, targetUserId, makeAdmin);
            return Ok(new { success });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
    }

    [HttpDelete("{chatId:guid}/members/{targetUserId:guid}")]
    public async Task<IActionResult> RemoveMember(Guid chatId, Guid targetUserId)
    {
        var userId = GetCurrentUserId();
        try
        {
            var success = await _chatService.RemoveMemberFromGroupAsync(userId, chatId, targetUserId);
            return Ok(new { success });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
    }

    [HttpDelete("{chatId:guid}")]
    public async Task<IActionResult> DeleteGroup(Guid chatId)
    {
        var userId = GetCurrentUserId();
        try
        {
            var success = await _chatService.DeleteGroupAsync(userId, chatId);
            return Ok(new { success });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
    }

    private Guid GetCurrentUserId()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(userIdStr, out var userId))
        {
            return userId;
        }
        throw new UnauthorizedAccessException("User identity not found in token.");
    }
}
