// PB20/PB26 (Wallace) - notifications: save in-app message and send email copy
using System.Net;
using System.Text.RegularExpressions;
using FoodOrderingSystem.Data;
using FoodOrderingSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace FoodOrderingSystem.Services;

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        ApplicationDbContext context,
        IEmailService emailService,
        ILogger<NotificationService> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    // PB20: save an in-app notification, then email a copy (PB26)
    public async Task SendAsync(string userId, string type, string message)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(message))
            return;

        // 1. Clean the input to fit the columns (type 50, message 500 chars)
        var trimmedType = string.IsNullOrWhiteSpace(type) ? "General" : type.Trim();
        if (trimmedType.Length > 50)
            trimmedType = trimmedType[..50];

        var trimmedMessage = message.Trim();
        if (trimmedMessage.Length > 500)
            trimmedMessage = trimmedMessage[..500];

        // 2. Save it as unread; the navbar bell counts unread ones
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

        // PB26: Send email confirmation for booking, order, and payment notifications.
        // Wrapped in try/catch so email issues never break the caller's transaction.
        try
        {
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user != null && !string.IsNullOrWhiteSpace(user.Email))
            {
                var (subject, htmlBody) = BuildConfirmationEmail(
                    user.FullName,
                    trimmedType,
                    trimmedMessage,
                    notification.CreatedAt);

                await _emailService.SendEmailAsync(user.Email, subject, htmlBody, isHtml: true);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "PB26: Could not dispatch email notification for user {UserId}.", userId);
        }
    }

    // PB20: my notifications, newest first
    public async Task<List<Notification>> GetForUserAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return new List<Notification>();

        return await _context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    // PB20: number on the navbar bell
    public async Task<int> GetUnreadCountAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return 0;

        return await _context.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead);
    }

    // PB20: mark one as read (only the owner's, when userId is given)
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

    // PB20: mark all of mine as read
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

    // PB26: build the email subject and HTML body from the notification text
    private static (string Subject, string HtmlBody) BuildConfirmationEmail(
        string customerName,
        string type,
        string message,
        DateTime createdAtUtc)
    {
        // Extract Reference Number (e.g. "#12" or "R-000012") if present in the notification text
        var refMatch = Regex.Match(message, @"(R-\d{6}|#\d+)");
        string referenceNumber = refMatch.Success ? refMatch.Value : "N/A";

        // Extract Amount (e.g. "RM 45.00" or "RM45.00") if present in the notification text
        var amountMatch = Regex.Match(message, @"RM\s?\d+(?:,\d{3})*(?:\.\d{2})?");
        string amountText = amountMatch.Success ? amountMatch.Value : string.Empty;

        string subjectPrefix = type.ToLowerInvariant() switch
        {
            "reservation" => "Reservation Confirmation & Update",
            "order" => "Order Confirmation & Update",
            "payment" => "Payment Receipt & Confirmation",
            _ => $"{type} Notification"
        };

        string subject = referenceNumber != "N/A"
            ? $"[Rumah Rasa] {subjectPrefix} ({referenceNumber})"
            : $"[Rumah Rasa] {subjectPrefix}";

        // HTML-encode everything put in the email, so text cannot inject markup
        string safeName = WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(customerName) ? "Valued Guest" : customerName);
        string safeType = WebUtility.HtmlEncode(type);
        string safeMessage = WebUtility.HtmlEncode(message);
        string timestampLocal = createdAtUtc.ToLocalTime().ToString("dd/MM/yyyy hh:mm tt");

        string amountRow = !string.IsNullOrEmpty(amountText)
            ? $"<tr><td style='padding:6px 12px;font-weight:bold;color:#555;'>Amount:</td><td style='padding:6px 12px;'>{WebUtility.HtmlEncode(amountText)}</td></tr>"
            : string.Empty;

        string html = $@"<!DOCTYPE html>
<html>
<head><meta charset='utf-8' /></head>
<body style='font-family:Segoe UI,Arial,sans-serif;color:#212529;line-height:1.5;'>
    <div style='max-width:600px;margin:0 auto;border:1px solid #dee2e6;border-radius:6px;overflow:hidden;'>
        <div style='background-color:#8b2635;color:#ffffff;padding:16px 20px;'>
            <h2 style='margin:0;font-size:20px;'>Rumah Rasa — {safeType} Confirmation</h2>
        </div>
        <div style='padding:20px;'>
            <p>Dear <strong>{safeName}</strong>,</p>
            <p>{safeMessage}</p>
            <table style='width:100%;border-collapse:collapse;margin:16px 0;background-color:#f8f9fa;border:1px solid #e9ecef;'>
                <tr>
                    <td style='padding:6px 12px;font-weight:bold;color:#555;width:40%;'>Category:</td>
                    <td style='padding:6px 12px;'>{safeType}</td>
                </tr>
                <tr>
                    <td style='padding:6px 12px;font-weight:bold;color:#555;'>Reference Number:</td>
                    <td style='padding:6px 12px;'>{WebUtility.HtmlEncode(referenceNumber)}</td>
                </tr>
                <tr>
                    <td style='padding:6px 12px;font-weight:bold;color:#555;'>Date &amp; Time:</td>
                    <td style='padding:6px 12px;'>{timestampLocal}</td>
                </tr>
                {amountRow}
            </table>
            <p style='font-size:13px;color:#6c757d;margin-top:20px;'>
                Thank you for dining with Rumah Rasa. You can view your full history anytime in your account portal.
            </p>
        </div>
    </div>
</body>
</html>";

        return (subject, html);
    }
}