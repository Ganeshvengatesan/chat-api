using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatApplicationAPI.Models;

public enum MessageType
{
    Text = 1,
    Image = 2,
    Voice = 3,
    Video = 4,
    File = 5,
    System = 6
}

public class Message
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid ChatId { get; set; }
    [ForeignKey(nameof(ChatId))]
    public Chat? Chat { get; set; }

    [Required]
    public Guid SenderId { get; set; }
    [ForeignKey(nameof(SenderId))]
    public User? Sender { get; set; }

    public string Content { get; set; } = string.Empty;
    public string MediaUrl { get; set; } = string.Empty;
    public MessageType Type { get; set; } = MessageType.Text;
    public bool IsRead { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
