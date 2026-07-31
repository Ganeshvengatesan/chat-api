using ChatApplicationAPI.DTOs.Friends;
using ChatApplicationAPI.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ChatApplicationAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class FriendsController : ControllerBase
{
    private readonly IFriendService _friendService;

    public FriendsController(IFriendService friendService)
    {
        _friendService = friendService;
    }

    [HttpPost("request")]
    public async Task<IActionResult> SendRequest([FromBody] SendFriendRequestDto request)
    {
        var senderId = GetUserId();
        try
        {
            var result = await _friendService.SendFriendOrMessageRequestAsync(senderId, request);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("respond")]
    public async Task<IActionResult> Respond([FromBody] RespondFriendRequestDto request)
    {
        var receiverId = GetUserId();
        var success = await _friendService.RespondToRequestAsync(receiverId, request);
        return Ok(new { success });
    }

    [HttpGet("requests")]
    public async Task<IActionResult> GetPendingRequests()
    {
        var userId = GetUserId();
        var list = await _friendService.GetPendingRequestsAsync(userId);
        return Ok(list);
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string query)
    {
        var userId = GetUserId();
        var results = await _friendService.SearchUsersAsync(userId, query);
        return Ok(results);
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
