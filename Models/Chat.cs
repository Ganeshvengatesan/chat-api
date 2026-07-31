using System.ComponentModel.DataAnnotations;

namespace ChatApplicationAPI.Models;

public enum ChatType
{
    Direct = 1,
    Group = 2,
    Channel = 3
}

public class Chat
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public ChatType Type { get; set; } = ChatType.Direct;
    public string Description { get; set; } = string.Empty;
    public string GroupIconUrl { get; set; } = string.Empty;
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<ChatParticipant> Participants { get; set; } = new List<ChatParticipant>();
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}
