using System;

namespace MyWebApi.Models;

public class Message
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public string SenderId { get; set; } = string.Empty;
    public string? ReceiverId { get; set; } // For private messages
    public int? GroupId { get; set; } // For group messages
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public bool IsEdited { get; set; }
    public DateTime? EditedAt { get; set; }
    public MessageType Type { get; set; } = MessageType.Text;
    public string? AttachmentUrl { get; set; }

    public virtual ApplicationUser Sender { get; set; } = null!;
    public virtual ApplicationUser? Receiver { get; set; }
    public virtual Group? Group { get; set; }
    public virtual ICollection<MessageRead> MessageReads { get; set; } = new List<MessageRead>();

    public enum MessageType
    {
        Text,
        Image,
        File,
        Audio,
        Video
    }

}
