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

    public async Task OnGetAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        Notifications = await _notificationService.GetForUserAsync(userId);
    }

    public async Task<IActionResult> OnPostMarkReadAsync(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        await _notificationService.MarkReadAsync(id, userId);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostMarkAllReadAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        await _notificationService.MarkAllReadAsync(userId);
        TempData["SuccessMessage"] = "All notifications marked as read.";
        return RedirectToPage();
    }
}