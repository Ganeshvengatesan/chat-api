using ChatApplicationAPI.Data;
using ChatApplicationAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ChatApplicationAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class StatusController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public StatusController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreateStatus([FromBody] CreateStatusDto request)
    {
        var userId = GetCurrentUserId();

        var status = new UserStatus
        {
            UserId = userId,
            Caption = request.Caption,
            MediaUrl = request.MediaUrl,
            BackgroundColor = string.IsNullOrEmpty(request.BackgroundColor) ? "#FF2A7A" : request.BackgroundColor,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        };

        await _db.UserStatuses.AddAsync(status);
        await _db.SaveChangesAsync();

        return Ok(status);
    }

    [HttpGet]
    public async Task<IActionResult> GetActiveStatuses()
    {
        var userId = GetCurrentUserId();
        var now = DateTime.UtcNow;

        var statuses = await _db.UserStatuses
            .Include(s => s.User)
            .Where(s => s.ExpiresAt > now)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new
            {
                statusId = s.StatusId,
                userId = s.UserId,
                userName = s.User != null ? s.User.FullName : "",
                userAvatar = s.User != null ? s.User.AvatarUrl : "",
                caption = s.Caption,
                mediaUrl = s.MediaUrl,
                backgroundColor = s.BackgroundColor,
                createdAt = s.CreatedAt
            })
            .ToListAsync();

        return Ok(statuses);
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

public class CreateStatusDto
{
    public string Caption { get; set; } = string.Empty;
    public string MediaUrl { get; set; } = string.Empty;
    public string BackgroundColor { get; set; } = "#FF2A7A";
}
