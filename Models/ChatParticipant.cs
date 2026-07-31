using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatApplicationAPI.Models;

public enum ParticipantRole
{
    Member = 1,
    Admin = 2,
    Owner = 3
}

public class ChatParticipant
{
    [Key]
    public Guid ParticipantId { get; set; } = Guid.NewGuid();

    [Required]
    public Guid ChatId { get; set; }
    [ForeignKey(nameof(ChatId))]
    public Chat? Chat { get; set; }

    [Required]
    public Guid UserId { get; set; }
    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    public ParticipantRole Role { get; set; } = ParticipantRole.Member;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}
