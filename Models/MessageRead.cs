using System;

namespace MyWebApi.Models;

public class MessageRead
{
    public int Id { get; set; }
    public int MessageId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime ReadAt { get; set; } = DateTime.UtcNow;

    public virtual Message Message { get; set; } = null!;
    public virtual ApplicationUser User { get; set; } = null!;

}
