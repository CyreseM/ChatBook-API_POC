using MyWebApi.Models;
using static MyWebApi.Models.Message;

namespace MyWebApi.DTOs
{
    public class SendMessageDto
    {
        public string Content { get; set; } = string.Empty;
        public string? ReceiverId { get; set; }
        public int? GroupId { get; set; }
        public MessageType Type { get; set; } = MessageType.Text;
        public string? AttachmentUrl { get; set; }
    }

    public class MessageDto
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public string SenderId { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
        public string? ReceiverId { get; set; }
        public int? GroupId { get; set; }
        public DateTime SentAt { get; set; }
        public bool IsEdited { get; set; }
        public DateTime? EditedAt { get; set; }
        public MessageType Type { get; set; }
        public string? AttachmentUrl { get; set; }
        public bool IsRead { get; set; }
    }
}
