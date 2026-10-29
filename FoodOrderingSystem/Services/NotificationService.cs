using FoodOrderingSystem.Data;
using FoodOrderingSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace FoodOrderingSystem.Services;

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _context;

    public NotificationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task SendAsync(string userId, string type, string message)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(message))
            return;

        var trimmedType = string.IsNullOrWhiteSpace(type) ? "General" : type.Trim();
        if (trimmedType.Length > 50)
            trimmedType = trimmedType[..50];

        var trimmedMessage = message.Trim();
        if (trimmedMessage.Length > 500)
            trimmedMessage = trimmedMessage[..500];

        var notification = new Notification
        {
            UserId = userId,
            Type = trimmedType,
            Message = trimmedMessage,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();
    }

    public async Task<List<Notification>> GetForUserAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return new List<Notification>();

        return await _context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    public async Task<int> GetUnreadCountAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return 0;

        return await _context.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead);
    }

    public async Task MarkReadAsync(int id, string? userId = null)
    {
        var query = _context.Notifications.Where(n => n.NotificationId == id);

        if (!string.IsNullOrWhiteSpace(userId))
        {
            query = query.Where(n => n.UserId == userId);
        }

        var notification = await query.FirstOrDefaultAsync();
        if (notification != null && !notification.IsRead)
        {
            notification.IsRead = true;
            await _context.SaveChangesAsync();
        }
    }

    public async Task MarkAllReadAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return;

        var unread = await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();

        if (unread.Count == 0)
            return;

        foreach (var n in unread)
        {
            n.IsRead = true;
        }

        await _context.SaveChangesAsync();
    }
}