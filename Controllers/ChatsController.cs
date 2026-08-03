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
    private readonly IWebHostEnvironment _environment;
    private readonly IS3StorageService _s3StorageService;

    public ChatsController(IChatService chatService, IWebHostEnvironment environment, IS3StorageService s3StorageService)
    {
        _chatService = chatService;
        _environment = environment;
        _s3StorageService = s3StorageService;
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

    [HttpPost("messages/{messageId:guid}/react")]
    public async Task<IActionResult> ReactToMessage(Guid messageId, [FromBody] ReactMessageDto request)
    {
        var userId = GetCurrentUserId();
        try
        {
            var msg = await _chatService.ReactToMessageAsync(userId, messageId, request.Reaction);
            return Ok(msg);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("messages/{messageId:guid}")]
    public async Task<IActionResult> DeleteMessage(Guid messageId)
    {
        var userId = GetCurrentUserId();
        try
        {
            var success = await _chatService.DeleteMessageAsync(userId, messageId);
            return Ok(new { success });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadMedia([FromForm] IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "No file provided." });
        }

        var fileExt = Path.GetExtension(file.FileName).ToLowerInvariant();
        var sizeInMb = (file.Length / (1024.0 * 1024.0)).ToString("0.1");

        string mediaType = "file";
        string s3Folder = "documents";

        var imageExts = new[] { ".png", ".jpg", ".jpeg", ".gif", ".webp", ".bmp", ".svg" };
        var videoExts = new[] { ".mp4", ".mov", ".avi", ".mkv", ".webm", ".3gp" };
        var audioExts = new[] { ".mp3", ".wav", ".m4a", ".aac", ".ogg" };
        var docExts = new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".zip", ".rar" };

        if (imageExts.Contains(fileExt))
        {
            mediaType = "image";
            s3Folder = "images";
        }
        else if (videoExts.Contains(fileExt))
        {
            mediaType = "video";
            s3Folder = "videos";
        }
        else if (audioExts.Contains(fileExt))
        {
            mediaType = "audio";
            s3Folder = "voice";
        }
        else if (docExts.Contains(fileExt))
        {
            mediaType = "document";
            s3Folder = "documents";
        }

        // 1. Try uploading to AWS S3
        string? fileUrl = await _s3StorageService.UploadFileAsync(file, s3Folder);

        // 2. Fallback to local server disk storage if S3 is unconfigured or offline
        if (string.IsNullOrEmpty(fileUrl))
        {
            var contentRoot = _environment.ContentRootPath;
            var uploadsFolder = Path.Combine(contentRoot, "wwwroot", "uploads");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var fileName = $"{Guid.NewGuid()}{fileExt}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            fileUrl = $"/uploads/{fileName}";
        }

        return Ok(new
        {
            url = fileUrl,
            fileName = file.FileName,
            fileSize = $"{sizeInMb} MB",
            mediaType = mediaType
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
