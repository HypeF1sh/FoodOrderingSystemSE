using FoodOrderingSystem.Data;
using FoodOrderingSystem.Services;
using FoodOrderingSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FoodOrderingSystem.Pages.Admin.Menu;

[Authorize(Roles = DbSeeder.AdminRole)]
public class DeleteCategoryModel : PageModel
{
    private readonly IMenuService _menuService;

    public DeleteCategoryModel(IMenuService menuService)
    {
        _menuService = menuService;
    }

    public AdminMenuCategoryRowViewModel? Category { get; set; }

    // When set, the category cannot be deleted: the page shows why and disables the button
    public string? BlockReason { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        if (!await LoadAsync(id)) return NotFound();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        // The service checks the delete rule again, so a stale page or crafted POST cannot bypass it
        var (success, message) = await _menuService.DeleteCategoryAsync(id);
        if (!success)
        {
            if (!await LoadAsync(id)) return NotFound();
            ModelState.AddModelError(string.Empty, message);
            return Page();
        }

        TempData["SuccessMessage"] = message;
        return RedirectToPage("./Index", new { tab = "categories" });
    }

    private async Task<bool> LoadAsync(int id)
    {
        Category = await _menuService.GetCategoryRowAsync(id);
        if (Category == null) return false;

        BlockReason = await _menuService.GetCategoryDeleteBlockReasonAsync(id);
        return true;
    }
}
