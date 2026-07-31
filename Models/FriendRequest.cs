using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatApplicationAPI.Models;

public enum RequestStatus
{
    Pending = 1,
    Accepted = 2,
    Rejected = 3,
    Blocked = 4
}

public class FriendRequest
{
    [Key]
    public Guid RequestId { get; set; } = Guid.NewGuid();

    [Required]
    public Guid SenderId { get; set; }
    [ForeignKey(nameof(SenderId))]
    public User? Sender { get; set; }

    [Required]
    public Guid ReceiverId { get; set; }
    [ForeignKey(nameof(ReceiverId))]
    public User? Receiver { get; set; }

    public RequestStatus Status { get; set; } = RequestStatus.Pending;
    public string InitialMessage { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
