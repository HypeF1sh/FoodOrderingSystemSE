using FoodOrderingSystem.Data;
using FoodOrderingSystem.Services;
using FoodOrderingSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FoodOrderingSystem.Pages.Admin.Menu;

[Authorize(Roles = DbSeeder.AdminRole)]
public class CreateItemModel : PageModel
{
    private readonly IMenuService _menuService;

    public CreateItemModel(IMenuService menuService)
    {
        _menuService = menuService;
    }

    [BindProperty]
    public MenuItemInput Input { get; set; } = new();

    public List<SelectListItem> CategoryOptions { get; set; } = new();

    public async Task OnGetAsync()
    {
        await LoadCategoryOptionsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // 1. Field checks from the data annotations on MenuItemInput
        if (!ModelState.IsValid)
        {
            await LoadCategoryOptionsAsync();
            return Page();
        }

        // 2. Business checks + save happen in the service
        var (success, message) = await _menuService.CreateItemAsync(Input);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, message);
            await LoadCategoryOptionsAsync();
            return Page();
        }

        // 3. Post/Redirect/Get: the banner shows on the list page
        TempData["SuccessMessage"] = message;
        return RedirectToPage("./Index");
    }

    // Dropdown options are rebuilt on every render because they are not posted back
    private async Task LoadCategoryOptionsAsync()
    {
        var categories = await _menuService.GetCategoryOptionsAsync();
        CategoryOptions = categories
            .Select(c => new SelectListItem(c.IsActive ? c.Name : $"{c.Name} (hidden)", c.MenuCategoryId.ToString()))
            .ToList();
    }
}
