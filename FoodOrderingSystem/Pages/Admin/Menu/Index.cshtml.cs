using FoodOrderingSystem.Data;
using FoodOrderingSystem.Services;
using FoodOrderingSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FoodOrderingSystem.Pages.Admin.Menu;

// The /Admin folder is already admin-only (Program.cs); the attribute makes it explicit on the page too
[Authorize(Roles = DbSeeder.AdminRole)]
public class IndexModel : PageModel
{
    private readonly IMenuService _menuService;

    public IndexModel(IMenuService menuService)
    {
        _menuService = menuService;
    }

    public List<AdminMenuItemRowViewModel> Items { get; set; } = new();
    public List<AdminMenuCategoryRowViewModel> Categories { get; set; } = new();

    // Which tab is open: /Admin/Menu (items) or /Admin/Menu?tab=categories
    public bool ShowCategories { get; set; }

    public async Task OnGetAsync(string? tab)
    {
        ShowCategories = tab == "categories";

        // Load both lists so switching tabs needs no extra request
        Items = await _menuService.GetAllItemsForAdminAsync();
        Categories = await _menuService.GetAllCategoriesForAdminAsync();
    }

    // One-click "Mark sold out / available" button on each item row
    public async Task<IActionResult> OnPostToggleAsync(int id)
    {
        var (success, message) = await _menuService.ToggleItemAvailabilityAsync(id);

        TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
        return RedirectToPage();
    }
}
