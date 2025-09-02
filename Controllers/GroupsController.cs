using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyWebApi.Data;
using MyWebApi.DTOs;
using MyWebApi.Models;

namespace MyWebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GroupsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public GroupsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> CreateGroup([FromBody] CreateGroupDto groupDto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var group = new Group
        {
            Name = groupDto.Name,
            Description = groupDto.Description,
            IsPrivate = groupDto.IsPrivate,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Groups.Add(group);
        await _context.SaveChangesAsync();

        // Add creator as owner
        _context.GroupMembers.Add(new GroupMember
        {
            GroupId = group.Id,
            UserId = userId,
            Role = GroupRole.Owner,
            JoinedAt = DateTime.UtcNow
        });

        // Add other members
        foreach (var memberId in groupDto.MemberIds)
        {
            if (memberId != userId)
            {
                _context.GroupMembers.Add(new GroupMember
                {
                    GroupId = group.Id,
                    UserId = memberId,
                    Role = GroupRole.Member,
                    JoinedAt = DateTime.UtcNow
                });
            }
        }

        await _context.SaveChangesAsync();

        var result = await GetGroupById(group.Id);
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetUserGroups()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var groups = await _context.GroupMembers
            .Where(gm => gm.UserId == userId)
            .Include(gm => gm.Group)
                .ThenInclude(g => g.Members)
                    .ThenInclude(m => m.User)
            .Include(gm => gm.Group.Messages.OrderByDescending(m => m.SentAt).Take(1))
                .ThenInclude(m => m.Sender)
            .Select(gm => new GroupDto
            {
                Id = gm.Group.Id,
                Name = gm.Group.Name,
                Description = gm.Group.Description,
                GroupPicture = gm.Group.GroupPicture,
                CreatedBy = gm.Group.CreatedBy,
                CreatedAt = gm.Group.CreatedAt,
                IsPrivate = gm.Group.IsPrivate,
                Members = gm.Group.Members.Select(m => new GroupMemberDto
                {
                    UserId = m.UserId,
                    UserName = $"{m.User.FirstName} {m.User.LastName}",
                    Role = m.Role,
                    JoinedAt = m.JoinedAt,
                    IsOnline = m.User.IsOnline
                }).ToList(),
                LastMessage = gm.Group.Messages.FirstOrDefault() != null ? new MessageDto
                {
                    Id = gm.Group.Messages.First().Id,
                    Content = gm.Group.Messages.First().Content,
                    SenderId = gm.Group.Messages.First().SenderId,
                    SenderName = $"{gm.Group.Messages.First().Sender.FirstName} {gm.Group.Messages.First().Sender.LastName}",
                    SentAt = gm.Group.Messages.First().SentAt,
                    Type = gm.Group.Messages.First().Type
                } : null
            })
            .ToListAsync();

        return Ok(groups);
    }

    [HttpGet("{groupId}")]
    public async Task<IActionResult> GetGroupById(int groupId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        // Verify user is member of the group
        var isMember = await _context.GroupMembers
            .AnyAsync(gm => gm.GroupId == groupId && gm.UserId == userId);

        if (!isMember)
            return Forbid();

        var group = await _context.Groups
            .Include(g => g.Members)
                .ThenInclude(m => m.User)
            .Include(g => g.Messages.OrderByDescending(m => m.SentAt).Take(1))
                .ThenInclude(m => m.Sender)
            .FirstOrDefaultAsync(g => g.Id == groupId);

        if (group == null)
            return NotFound();

        var groupDto = new GroupDto
        {
            Id = group.Id,
            Name = group.Name,
            Description = group.Description,
            GroupPicture = group.GroupPicture,
            CreatedBy = group.CreatedBy,
            CreatedAt = group.CreatedAt,
            IsPrivate = group.IsPrivate,
            Members = group.Members.Select(m => new GroupMemberDto
            {
                UserId = m.UserId,
                UserName = $"{m.User.FirstName} {m.User.LastName}",
                Role = m.Role,
                JoinedAt = m.JoinedAt,
                IsOnline = m.User.IsOnline
            }).ToList(),
            LastMessage = group.Messages.FirstOrDefault() != null ? new MessageDto
            {
                Id = group.Messages.First().Id,
                Content = group.Messages.First().Content,
                SenderId = group.Messages.First().SenderId,
                SenderName = $"{group.Messages.First().Sender.FirstName} {group.Messages.First().Sender.LastName}",
                SentAt = group.Messages.First().SentAt,
                Type = group.Messages.First().Type
            } : null
        };

        return Ok(groupDto);
    }

    [HttpPost("{groupId}/members")]
    public async Task<IActionResult> AddMember(int groupId, [FromBody] string newMemberId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        // Verify user is admin or owner of the group
        var userMembership = await _context.GroupMembers
            .FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == userId);

        if (userMembership == null || (userMembership.Role != GroupRole.Admin && userMembership.Role != GroupRole.Owner))
            return Forbid();

        // Check if user is already a member
        var existingMembership = await _context.GroupMembers
            .FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == newMemberId);

        if (existingMembership != null)
            return BadRequest("User is already a member of this group");

        _context.GroupMembers.Add(new GroupMember
        {
            GroupId = groupId,
            UserId = newMemberId,
            Role = GroupRole.Member,
            JoinedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        return Ok();
    }

    [HttpDelete("{groupId}/members/{memberId}")]
    public async Task<IActionResult> RemoveMember(int groupId, string memberId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        // Verify user is admin or owner of the group or removing themselves
        var userMembership = await _context.GroupMembers
            .FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == userId);

        if (userMembership == null)
            return Forbid();

        if (userId != memberId && userMembership.Role != GroupRole.Admin && userMembership.Role != GroupRole.Owner)
            return Forbid();

        var memberToRemove = await _context.GroupMembers
            .FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == memberId);

        if (memberToRemove == null)
            return NotFound();

        _context.GroupMembers.Remove(memberToRemove);
        await _context.SaveChangesAsync();

        return Ok();
    }

    [HttpPut("{groupId}")]
    public async Task<IActionResult> UpdateGroup(int groupId, [FromBody] CreateGroupDto groupDto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        // Verify user is admin or owner of the group
        var userMembership = await _context.GroupMembers
            .FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == userId);

        if (userMembership == null || (userMembership.Role != GroupRole.Admin && userMembership.Role != GroupRole.Owner))
            return Forbid();

        var group = await _context.Groups.FindAsync(groupId);
        if (group == null)
            return NotFound();

        group.Name = groupDto.Name;
        group.Description = groupDto.Description;
        group.IsPrivate = groupDto.IsPrivate;

        await _context.SaveChangesAsync();
        return Ok();
    }

    [HttpDelete("{groupId}")]
    public async Task<IActionResult> DeleteGroup(int groupId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        // Verify user is owner of the group
        var userMembership = await _context.GroupMembers
            .FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == userId);

        if (userMembership == null || userMembership.Role != GroupRole.Owner)
            return Forbid();

        var group = await _context.Groups.FindAsync(groupId);
        if (group == null)
            return NotFound();

        _context.Groups.Remove(group);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}