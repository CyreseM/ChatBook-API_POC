using MyWebApi.Models;

namespace MyWebApi.DTOs
{
    public class CreateGroupDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsPrivate { get; set; }
        public List<string> MemberIds { get; set; } = new List<string>();
    }

    public class GroupDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? GroupPicture { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsPrivate { get; set; }
        public List<GroupMemberDto> Members { get; set; } = new List<GroupMemberDto>();
        public MessageDto? LastMessage { get; set; }
    }

    public class GroupMemberDto
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public GroupRole Role { get; set; }
        public DateTime JoinedAt { get; set; }
        public bool IsOnline { get; set; }
    }
}
