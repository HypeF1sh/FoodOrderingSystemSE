// PB06 (Jordan) - delete a dish (blocked if it was ever ordered)
using FoodOrderingSystem.Data;
using FoodOrderingSystem.Services;
using FoodOrderingSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FoodOrderingSystem.Pages.Admin.Menu;

[Authorize(Roles = DbSeeder.AdminRole)]
public class DeleteItemModel : PageModel
{
    private readonly IMenuService _menuService;

    public DeleteItemModel(IMenuService menuService)
    {
        _menuService = menuService;
    }

    public AdminMenuItemRowViewModel? Item { get; set; }

    // When set, the item cannot be deleted: the page shows why and disables the button
    public string? BlockReason { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        if (!await LoadAsync(id)) return NotFound();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        // The service checks the delete rule again, so a stale page or crafted POST cannot bypass it
        var (success, message) = await _menuService.DeleteItemAsync(id);
        if (!success)
        {
            if (!await LoadAsync(id)) return NotFound();
            ModelState.AddModelError(string.Empty, message);
            return Page();
        }

        TempData["SuccessMessage"] = message;
        return RedirectToPage("./Index");
    }

    private async Task<bool> LoadAsync(int id)
    {
        Item = await _menuService.GetItemRowAsync(id);
        if (Item == null) return false;

        BlockReason = await _menuService.GetItemDeleteBlockReasonAsync(id);
        return true;
    }
}
