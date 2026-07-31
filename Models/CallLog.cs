using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatApplicationAPI.Models;

public enum CallType
{
    Voice = 1,
    Video = 2
}

public enum CallStatus
{
    Completed = 1,
    Missed = 2,
    Rejected = 3,
    Ongoing = 4
}

public class CallLog
{
    [Key]
    public Guid CallId { get; set; } = Guid.NewGuid();

    [Required]
    public Guid CallerId { get; set; }
    [ForeignKey(nameof(CallerId))]
    public User? Caller { get; set; }

    [Required]
    public Guid ReceiverId { get; set; }
    [ForeignKey(nameof(ReceiverId))]
    public User? Receiver { get; set; }

    public CallType Type { get; set; } = CallType.Voice;
    public CallStatus Status { get; set; } = CallStatus.Completed;
    public int DurationSeconds { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
