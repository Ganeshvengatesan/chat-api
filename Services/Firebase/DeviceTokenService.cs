using ChatApplicationAPI.Data;
using ChatApplicationAPI.DTOs.Notifications;
using ChatApplicationAPI.Interfaces;
using ChatApplicationAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace ChatApplicationAPI.Services.Firebase;

public class DeviceTokenService : IDeviceTokenService
{
    private readonly ApplicationDbContext _db;

    public DeviceTokenService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<bool> RegisterOrUpdateDeviceAsync(Guid userId, DeviceTokenRequest request)
    {
        var existingDevice = await _db.UserDevices
            .FirstOrDefaultAsync(d => d.UserId == userId && d.DeviceToken == request.DeviceToken);

        if (existingDevice != null)
        {
            existingDevice.Platform = request.Platform;
            existingDevice.DeviceName = request.DeviceName;
            existingDevice.AppVersion = request.AppVersion;
            existingDevice.IsActive = true;
            existingDevice.LastLogin = DateTime.UtcNow;
        }
        else
        {
            var device = new UserDevice
            {
                UserId = userId,
                DeviceToken = request.DeviceToken,
                Platform = request.Platform,
                DeviceName = request.DeviceName,
                AppVersion = request.AppVersion,
                IsActive = true,
                LastLogin = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };
            await _db.UserDevices.AddAsync(device);
        }

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<List<UserDevice>> GetUserActiveDevicesAsync(Guid userId)
    {
        return await _db.UserDevices
            .Where(d => d.UserId == userId && d.IsActive)
            .ToListAsync();
    }

    public async Task<bool> DeactivateDeviceAsync(Guid userId, string deviceToken)
    {
        var device = await _db.UserDevices
            .FirstOrDefaultAsync(d => d.UserId == userId && d.DeviceToken == deviceToken);

        if (device != null)
        {
            device.IsActive = false;
            await _db.SaveChangesAsync();
            return true;
        }
        return false;
    }
}
