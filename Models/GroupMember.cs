using System;

namespace MyWebApi.Models;

public class GroupMember
{
    public int Id { get; set; }
    public int GroupId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public GroupRole Role { get; set; } = GroupRole.Member;

    public virtual Group Group { get; set; } = null!;
    public virtual ApplicationUser User { get; set; } = null!;
}


public enum GroupRole
{
    Member,
    Admin,
    Owner
}
