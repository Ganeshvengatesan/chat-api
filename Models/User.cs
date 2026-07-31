using System.ComponentModel.DataAnnotations;

namespace ChatApplicationAPI.Models;

public class User
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public string StatusBio { get; set; } = "Live, Love, Link";
    public bool IsOnline { get; set; } = false;
    public DateTime LastSeen { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<ChatParticipant> ChatParticipants { get; set; } = new List<ChatParticipant>();
    public ICollection<UserDevice> Devices { get; set; } = new List<UserDevice>();
}
