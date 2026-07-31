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
public class CallsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public CallsController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetCallHistory()
    {
        var userId = GetCurrentUserId();

        var calls = await _db.CallLogs
            .Include(c => c.Caller)
            .Include(c => c.Receiver)
            .Where(c => c.CallerId == userId || c.ReceiverId == userId)
            .OrderByDescending(c => c.CreatedAt)
            .Take(50)
            .Select(c => new
            {
                callId = c.CallId,
                isOutgoing = c.CallerId == userId,
                otherUserId = c.CallerId == userId ? c.ReceiverId : c.CallerId,
                otherUserName = c.CallerId == userId ? (c.Receiver != null ? c.Receiver.FullName : "") : (c.Caller != null ? c.Caller.FullName : ""),
                otherUserAvatar = c.CallerId == userId ? (c.Receiver != null ? c.Receiver.AvatarUrl : "") : (c.Caller != null ? c.Caller.AvatarUrl : ""),
                type = c.Type.ToString(),
                status = c.Status.ToString(),
                durationSeconds = c.DurationSeconds,
                createdAt = c.CreatedAt
            })
            .ToListAsync();

        return Ok(calls);
    }

    [HttpPost("log")]
    public async Task<IActionResult> LogCall([FromBody] LogCallDto request)
    {
        var userId = GetCurrentUserId();

        var log = new CallLog
        {
            CallerId = userId,
            ReceiverId = request.ReceiverId,
            Type = (CallType)request.Type,
            Status = (CallStatus)request.Status,
            DurationSeconds = request.DurationSeconds,
            CreatedAt = DateTime.UtcNow
        };

        await _db.CallLogs.AddAsync(log);
        await _db.SaveChangesAsync();

        return Ok(log);
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

public class LogCallDto
{
    public Guid ReceiverId { get; set; }
    public int Type { get; set; } = 1; // 1 = Voice, 2 = Video
    public int Status { get; set; } = 1; // 1 = Completed, 2 = Missed, 3 = Rejected
    public int DurationSeconds { get; set; } = 0;
}
