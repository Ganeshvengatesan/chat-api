using ChatApplicationAPI.DTOs.Notifications;
using ChatApplicationAPI.Models;

namespace ChatApplicationAPI.Interfaces;

public interface IDeviceTokenService
{
    Task<bool> RegisterOrUpdateDeviceAsync(Guid userId, DeviceTokenRequest request);
    Task<List<UserDevice>> GetUserActiveDevicesAsync(Guid userId);
    Task<bool> DeactivateDeviceAsync(Guid userId, string deviceToken);
}
