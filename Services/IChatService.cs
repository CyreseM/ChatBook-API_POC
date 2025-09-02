using System;
using MyWebApi.DTOs;

namespace MyWebApi.Services;

public interface IChatService
{
    Task<MessageDto> SendMessageAsync(string senderId, SendMessageDto messageDto);
    Task<List<MessageDto>> GetPrivateMessagesAsync(string userId1, string userId2, int page = 1, int pageSize = 50);
    Task<List<MessageDto>> GetGroupMessagesAsync(int groupId, int page = 1, int pageSize = 50);
    Task<MessageDto> UpdateMessageAsync(int messageId, string newContent, string userId);
    Task<bool> DeleteMessageAsync(int messageId, string userId);
    Task MarkMessageAsReadAsync(int messageId, string userId);
    Task<List<UserDto>> GetOnlineUsersAsync();
}
