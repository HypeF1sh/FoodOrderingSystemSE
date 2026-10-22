using FoodOrderingSystem.Data;
using FoodOrderingSystem.Services;
using FoodOrderingSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FoodOrderingSystem.Pages.Admin.Menu;

[Authorize(Roles = DbSeeder.AdminRole)]
public class EditItemModel : PageModel
{
    private readonly IMenuService _menuService;

    public EditItemModel(IMenuService menuService)
    {
        _menuService = menuService;
    }

    [BindProperty]
    public MenuItemInput Input { get; set; } = new();

    public List<SelectListItem> CategoryOptions { get; set; } = new();

    // Shown as a preview; read from the database, never from the form
    public string CurrentImageUrl { get; set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        // 1. Load the item's current values into the form, or 404 if it does not exist
        var input = await _menuService.GetItemInputAsync(id);
        if (input == null) return NotFound();

        Input = input;
        await LoadPageDataAsync(id);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        // 1. The id comes from the URL, so the form cannot be tampered with to edit a different item
        Input.MenuItemId = id;

        // 2. Field checks from the data annotations
        if (!ModelState.IsValid)
        {
            await LoadPageDataAsync(id);
            return Page();
        }

        // 3. Business checks + save in the service
        var (success, message) = await _menuService.UpdateItemAsync(Input);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, message);
            await LoadPageDataAsync(id);
            return Page();
        }

        TempData["SuccessMessage"] = message;
        return RedirectToPage("./Index");
    }

    private async Task LoadPageDataAsync(int id)
    {
        var categories = await _menuService.GetCategoryOptionsAsync();
        CategoryOptions = categories
            .Select(c => new SelectListItem(c.IsActive ? c.Name : $"{c.Name} (hidden)", c.MenuCategoryId.ToString()))
            .ToList();

        var row = await _menuService.GetItemRowAsync(id);
        CurrentImageUrl = row?.ImageUrl ?? string.Empty;
    }
}
