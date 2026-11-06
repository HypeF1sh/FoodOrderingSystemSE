// PB20 (Wallace) - my notifications: list, mark as read
using System.Security.Claims;
using FoodOrderingSystem.Models;
using FoodOrderingSystem.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FoodOrderingSystem.Pages.Notifications;

public class IndexModel : PageModel
{
    private readonly INotificationService _notificationService;

    public IndexModel(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public List<Notification> Notifications { get; set; } = new();

    // GET: my notifications
    public async Task OnGetAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        Notifications = await _notificationService.GetForUserAsync(userId);
    }

    // POST: mark one as read (only mine)
    public async Task<IActionResult> OnPostMarkReadAsync(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        await _notificationService.MarkReadAsync(id, userId);
        return RedirectToPage();
    }

    // POST: mark all mine as read
    public async Task<IActionResult> OnPostMarkAllReadAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        await _notificationService.MarkAllReadAsync(userId);
        TempData["SuccessMessage"] = "All notifications marked as read.";
        return RedirectToPage();
    }
}