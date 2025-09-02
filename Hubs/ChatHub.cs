using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MyWebApi.Data;
using MyWebApi.DTOs;
using MyWebApi.Services;
using static MyWebApi.Models.Message;

namespace MyWebApi.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly IChatService _chatService;
    private readonly INotificationService _notificationService;
    private readonly ApplicationDbContext _context;
    private static readonly Dictionary<string, string> UserConnections = new();

    public ChatHub(IChatService chatService, INotificationService notificationService, ApplicationDbContext context)
    {
        _chatService = chatService;
        _notificationService = notificationService;
        _context = context;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(userId))
        {
            UserConnections[userId] = Context.ConnectionId;

            // Update user online status
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.IsOnline = true;
                user.LastSeen = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            // Join user to their groups
            var userGroups = await _context.GroupMembers
                .Where(gm => gm.UserId == userId)
                .Select(gm => gm.GroupId.ToString())
                .ToListAsync();

            foreach (var groupId in userGroups)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"Group_{groupId}");
            }

            // Notify others that user is online
            await Clients.All.SendAsync("UserStatusChanged", userId, true);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(userId))
        {
            UserConnections.Remove(userId);

            // Update user offline status
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.IsOnline = false;
                user.LastSeen = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            // Notify others that user is offline
            await Clients.All.SendAsync("UserStatusChanged", userId, false);
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendPrivateMessage(string receiverId, string content, MessageType type = MessageType.Text, string? attachmentUrl = null)
    {
        var senderId = Context.UserIdentifier!;

        var messageDto = new SendMessageDto
        {
            Content = content,
            ReceiverId = receiverId,
            Type = type,
            AttachmentUrl = attachmentUrl
        };

        var message = await _chatService.SendMessageAsync(senderId, messageDto);

        // Send to receiver if online
        if (UserConnections.TryGetValue(receiverId, out var receiverConnectionId))
        {
            await Clients.Client(receiverConnectionId).SendAsync("ReceivePrivateMessage", message);
        }

        // Send confirmation to sender
        await Clients.Caller.SendAsync("MessageSent", message);
    }

    public async Task SendGroupMessage(int groupId, string content, MessageType type = MessageType.Text, string? attachmentUrl = null)
    {
        var senderId = Context.UserIdentifier!;

        // Verify user is member of the group
        var isMember = await _context.GroupMembers
            .AnyAsync(gm => gm.GroupId == groupId && gm.UserId == senderId);

        if (!isMember)
        {
            await Clients.Caller.SendAsync("Error", "You are not a member of this group");
            return;
        }

        var messageDto = new SendMessageDto
        {
            Content = content,
            GroupId = groupId,
            Type = type,
            AttachmentUrl = attachmentUrl
        };

        var message = await _chatService.SendMessageAsync(senderId, messageDto);

        // Send to all group members
        await Clients.Group($"Group_{groupId}").SendAsync("ReceiveGroupMessage", message);
    }

    public async Task JoinGroup(int groupId)
    {
        var userId = Context.UserIdentifier!;

        // Verify user is member of the group
        var isMember = await _context.GroupMembers
            .AnyAsync(gm => gm.GroupId == groupId && gm.UserId == userId);

        if (isMember)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Group_{groupId}");
            await Clients.Caller.SendAsync("JoinedGroup", groupId);
        }
        else
        {
            await Clients.Caller.SendAsync("Error", "You are not a member of this group");
        }
    }

    public async Task LeaveGroup(int groupId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Group_{groupId}");
        await Clients.Caller.SendAsync("LeftGroup", groupId);
    }

    public async Task MarkMessageAsRead(int messageId)
    {
        var userId = Context.UserIdentifier!;
        await _chatService.MarkMessageAsReadAsync(messageId, userId);

        // Notify sender that message was read
        var message = await _context.Messages.FindAsync(messageId);
        if (message != null && UserConnections.TryGetValue(message.SenderId, out var senderConnectionId))
        {
            await Clients.Client(senderConnectionId).SendAsync("MessageRead", messageId, userId);
        }
    }

    public async Task StartTyping(string? receiverId = null, int? groupId = null)
    {
        var userId = Context.UserIdentifier!;
        var user = await _context.Users.FindAsync(userId);
        var userName = $"{user?.FirstName} {user?.LastName}";

        if (!string.IsNullOrEmpty(receiverId) && UserConnections.TryGetValue(receiverId, out var receiverConnectionId))
        {
            await Clients.Client(receiverConnectionId).SendAsync("UserTyping", userId, userName, true);
        }
        else if (groupId.HasValue)
        {
            await Clients.OthersInGroup($"Group_{groupId}").SendAsync("UserTypingInGroup", groupId, userId, userName, true);
        }
    }

    public async Task StopTyping(string? receiverId = null, int? groupId = null)
    {
        var userId = Context.UserIdentifier!;
        var user = await _context.Users.FindAsync(userId);
        var userName = $"{user?.FirstName} {user?.LastName}";

        if (!string.IsNullOrEmpty(receiverId) && UserConnections.TryGetValue(receiverId, out var receiverConnectionId))
        {
            await Clients.Client(receiverConnectionId).SendAsync("UserTyping", userId, userName, false);
        }
        else if (groupId.HasValue)
        {
            await Clients.OthersInGroup($"Group_{groupId}").SendAsync("UserTypingInGroup", groupId, userId, userName, false);
        }
    }
}