using FoodOrderingSystem.Models;       // <-- match your project namespace
using FoodOrderingSystem.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FoodOrderingSystem.Pages.Admin.Announcements;

public class IndexModel : PageModel
{
    private readonly IAnnouncementService _service;

    public IndexModel(IAnnouncementService service)
    {
        _service = service;
    }

    public List<Announcement> Announcements { get; set; } = new();

    [TempData] public string? SuccessMessage { get; set; }
    [TempData] public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        Announcements = await _service.GetAllAsync();
    }

    public async Task<IActionResult> OnPostToggleAsync(int id)
    {
        var (success, message) = await _service.ToggleActiveAsync(id);
        if (success) SuccessMessage = message; else ErrorMessage = message;
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var (success, message) = await _service.DeleteAsync(id);
        if (success) SuccessMessage = message; else ErrorMessage = message;
        return RedirectToPage();
    }
}
