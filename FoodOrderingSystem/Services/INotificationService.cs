// PB20 (Wallace) - notifications (interface)
using FoodOrderingSystem.Models;

namespace FoodOrderingSystem.Services;

public interface INotificationService
{
    /// <summary>
    /// PB20: Creates and stores a notification for the specified user.
    /// </summary>
    Task SendAsync(string userId, string type, string message);

    /// <summary>
    /// PB20: Returns all notifications for a user, newest first.
    /// </summary>
    Task<List<Notification>> GetForUserAsync(string userId);

    /// <summary>
    /// PB20: Returns the count of unread notifications for the navbar badge.
    /// </summary>
    Task<int> GetUnreadCountAsync(string userId);

    /// <summary>
    /// PB20: Marks a single notification as read.
    /// </summary>
    Task MarkReadAsync(int id, string? userId = null);

    /// <summary>
    /// PB20: Marks all unread notifications for a user as read.
    /// </summary>
    Task MarkAllReadAsync(string userId);
}