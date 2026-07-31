using ChatApplicationAPI.DTOs.Notifications;
using ChatApplicationAPI.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ChatApplicationAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class NotificationController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpPost("send")]
    public async Task<IActionResult> SendNotification([FromBody] NotificationRequest request)
    {
        var response = await _notificationService.CreateAndSendNotificationAsync(request);
        return Ok(response);
    }

    [HttpGet]
    public async Task<IActionResult> GetNotifications()
    {
        var userId = GetUserId();
        var list = await _notificationService.GetUserNotificationsAsync(userId);
        return Ok(list);
    }

    [HttpPut("read/{id:guid}")]
    public async Task<IActionResult> MarkRead(Guid id)
    {
        var userId = GetUserId();
        var success = await _notificationService.MarkAsReadAsync(id, userId);
        return Ok(new { success });
    }

    private Guid GetUserId()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(userIdStr, out var userId))
        {
            return userId;
        }
        throw new UnauthorizedAccessException("User identity not found in token.");
    }
}
