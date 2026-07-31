using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatApplicationAPI.Models;

public class UserDevice
{
    [Key]
    public Guid DeviceId { get; set; } = Guid.NewGuid();

    [Required]
    public Guid UserId { get; set; }
    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    [Required]
    public string DeviceToken { get; set; } = string.Empty;

    public string Platform { get; set; } = "Android";
    public string DeviceName { get; set; } = string.Empty;
    public string AppVersion { get; set; } = "1.0.0";
    public bool IsActive { get; set; } = true;
    public DateTime LastLogin { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
