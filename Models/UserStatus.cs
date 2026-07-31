using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatApplicationAPI.Models;

public class UserStatus
{
    [Key]
    public Guid StatusId { get; set; } = Guid.NewGuid();

    [Required]
    public Guid UserId { get; set; }
    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    public string MediaUrl { get; set; } = string.Empty;
    public string Caption { get; set; } = string.Empty;
    public string BackgroundColor { get; set; } = "#FF2A7A";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddHours(24);
}
