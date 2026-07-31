using ChatApplicationAPI.DTOs.Notifications;
using ChatApplicationAPI.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ChatApplicationAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class DeviceController : ControllerBase
{
    private readonly IDeviceTokenService _deviceTokenService;

    public DeviceController(IDeviceTokenService deviceTokenService)
    {
        _deviceTokenService = deviceTokenService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> RegisterDevice([FromBody] DeviceTokenRequest request)
    {
        var userId = GetUserId();
        var result = await _deviceTokenService.RegisterOrUpdateDeviceAsync(userId, request);
        return Ok(new { success = result, message = "Device token registered successfully." });
    }

    [HttpPut("token")]
    public async Task<IActionResult> UpdateToken([FromBody] DeviceTokenRequest request)
    {
        var userId = GetUserId();
        var result = await _deviceTokenService.RegisterOrUpdateDeviceAsync(userId, request);
        return Ok(new { success = result, message = "Device token updated." });
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
