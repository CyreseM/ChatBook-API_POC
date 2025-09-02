using System;

namespace MyWebApi.Models;

public class Notification
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? RelatedEntityId { get; set; }

    public virtual ApplicationUser User { get; set; } = null!;
}

public enum NotificationType
{
    Message,
    GroupInvitation,
    FriendRequest,
    System
}