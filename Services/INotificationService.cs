using System;
using MyWebApi.Models;

namespace MyWebApi.Services;

public interface INotificationService
{
    Task<Notification> CreateNotificationAsync(string userId, string title, string content, NotificationType type, string? relatedEntityId = null);
    Task<List<Notification>> GetUserNotificationsAsync(string userId, bool unreadOnly = false);
    Task MarkNotificationAsReadAsync(int notificationId, string userId);
    Task MarkAllNotificationsAsReadAsync(string userId);
}
