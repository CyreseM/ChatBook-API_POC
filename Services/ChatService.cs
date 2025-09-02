using System;
using Microsoft.EntityFrameworkCore;
using MyWebApi.Data;
using MyWebApi.DTOs;
using MyWebApi.Models;

namespace MyWebApi.Services;

public class ChatService : IChatService
{
    private readonly ApplicationDbContext _context;
    private readonly INotificationService _notificationService;

    public ChatService(ApplicationDbContext context, INotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    public async Task<MessageDto> SendMessageAsync(string senderId, SendMessageDto messageDto)
    {
        var message = new Message
        {
            Content = messageDto.Content,
            SenderId = senderId,
            ReceiverId = messageDto.ReceiverId,
            GroupId = messageDto.GroupId,
            Type = messageDto.Type,
            AttachmentUrl = messageDto.AttachmentUrl,
            SentAt = DateTime.UtcNow
        };

        _context.Messages.Add(message);
        await _context.SaveChangesAsync();

        // Load related data
        await _context.Entry(message)
            .Reference(m => m.Sender)
            .LoadAsync();

        // Create notification
        if (messageDto.ReceiverId != null)
        {
            await _notificationService.CreateNotificationAsync(
                messageDto.ReceiverId,
                "New Message",
                $"You received a message from {message.Sender.FirstName}",
                NotificationType.Message,
                message.Id.ToString()
            );
        }
        else if (messageDto.GroupId.HasValue)
        {
            var groupMembers = await _context.GroupMembers
                .Where(gm => gm.GroupId == messageDto.GroupId && gm.UserId != senderId)
                .Select(gm => gm.UserId)
                .ToListAsync();

            var group = await _context.Groups.FindAsync(messageDto.GroupId);
            foreach (var memberId in groupMembers)
            {
                await _notificationService.CreateNotificationAsync(
                    memberId,
                    "New Group Message",
                    $"New message in {group?.Name}",
                    NotificationType.Message,
                    message.Id.ToString()
                );
            }
        }

        return new MessageDto
        {
            Id = message.Id,
            Content = message.Content,
            SenderId = message.SenderId,
            SenderName = $"{message.Sender.FirstName} {message.Sender.LastName}",
            ReceiverId = message.ReceiverId,
            GroupId = message.GroupId,
            SentAt = message.SentAt,
            IsEdited = message.IsEdited,
            EditedAt = message.EditedAt,
            Type = message.Type,
            AttachmentUrl = message.AttachmentUrl
        };
    }

    public async Task<List<MessageDto>> GetPrivateMessagesAsync(string userId1, string userId2, int page = 1, int pageSize = 50)
    {
        var messages = await _context.Messages
            .Include(m => m.Sender)
            .Include(m => m.MessageReads)
            .Where(m => (m.SenderId == userId1 && m.ReceiverId == userId2) ||
                       (m.SenderId == userId2 && m.ReceiverId == userId1))
            .OrderByDescending(m => m.SentAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new MessageDto
            {
                Id = m.Id,
                Content = m.Content,
                SenderId = m.SenderId,
                SenderName = $"{m.Sender.FirstName} {m.Sender.LastName}",
                ReceiverId = m.ReceiverId,
                GroupId = m.GroupId,
                SentAt = m.SentAt,
                IsEdited = m.IsEdited,
                EditedAt = m.EditedAt,
                Type = m.Type,
                AttachmentUrl = m.AttachmentUrl,
                IsRead = m.MessageReads.Any(mr => mr.UserId == userId1 || mr.UserId == userId2)
            })
            .ToListAsync();

        return messages.OrderBy(m => m.SentAt).ToList();
    }

    public async Task<List<MessageDto>> GetGroupMessagesAsync(int groupId, int page = 1, int pageSize = 50)
    {
        var messages = await _context.Messages
            .Include(m => m.Sender)
            .Include(m => m.MessageReads)
            .Where(m => m.GroupId == groupId)
            .OrderByDescending(m => m.SentAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new MessageDto
            {
                Id = m.Id,
                Content = m.Content,
                SenderId = m.SenderId,
                SenderName = $"{m.Sender.FirstName} {m.Sender.LastName}",
                ReceiverId = m.ReceiverId,
                GroupId = m.GroupId,
                SentAt = m.SentAt,
                IsEdited = m.IsEdited,
                EditedAt = m.EditedAt,
                Type = m.Type,
                AttachmentUrl = m.AttachmentUrl,
                IsRead = m.MessageReads.Any()
            })
            .ToListAsync();

        return messages.OrderBy(m => m.SentAt).ToList();
    }

    public async Task<MessageDto> UpdateMessageAsync(int messageId, string newContent, string userId)
    {
        var message = await _context.Messages
            .Include(m => m.Sender)
            .FirstOrDefaultAsync(m => m.Id == messageId && m.SenderId == userId);

        if (message == null)
            throw new UnauthorizedAccessException("Message not found or you don't have permission to edit it.");

        message.Content = newContent;
        message.IsEdited = true;
        message.EditedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return new MessageDto
        {
            Id = message.Id,
            Content = message.Content,
            SenderId = message.SenderId,
            SenderName = $"{message.Sender.FirstName} {message.Sender.LastName}",
            ReceiverId = message.ReceiverId,
            GroupId = message.GroupId,
            SentAt = message.SentAt,
            IsEdited = message.IsEdited,
            EditedAt = message.EditedAt,
            Type = message.Type,
            AttachmentUrl = message.AttachmentUrl
        };
    }

    public async Task<bool> DeleteMessageAsync(int messageId, string userId)
    {
        var message = await _context.Messages
            .FirstOrDefaultAsync(m => m.Id == messageId && m.SenderId == userId);

        if (message == null)
            return false;

        _context.Messages.Remove(message);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task MarkMessageAsReadAsync(int messageId, string userId)
    {
        var existingRead = await _context.MessageReads
            .FirstOrDefaultAsync(mr => mr.MessageId == messageId && mr.UserId == userId);

        if (existingRead == null)
        {
            _context.MessageReads.Add(new MessageRead
            {
                MessageId = messageId,
                UserId = userId,
                ReadAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<UserDto>> GetOnlineUsersAsync()
    {
        return await _context.Users
            .Where(u => u.IsOnline)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Email = u.Email,
                FirstName = u.FirstName,
                LastName = u.LastName,
                ProfilePicture = u.ProfilePicture,
                IsOnline = u.IsOnline,
                LastSeen = u.LastSeen
            })
            .ToListAsync();
    }
}


