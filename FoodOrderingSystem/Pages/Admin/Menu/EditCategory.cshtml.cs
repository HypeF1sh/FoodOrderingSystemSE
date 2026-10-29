using FoodOrderingSystem.Data;
using FoodOrderingSystem.Services;
using FoodOrderingSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FoodOrderingSystem.Pages.Admin.Menu;

[Authorize(Roles = DbSeeder.AdminRole)]
public class EditCategoryModel : PageModel
{
    private readonly IMenuService _menuService;

    public EditCategoryModel(IMenuService menuService)
    {
        _menuService = menuService;
    }

    [BindProperty]
    public MenuCategoryInput Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var input = await _menuService.GetCategoryInputAsync(id);
        if (input == null) return NotFound();

        Input = input;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        // 1. The id comes from the URL, not the form
        Input.MenuCategoryId = id;

        // 2. Field checks from the data annotations
        if (!ModelState.IsValid) return Page();

        // 3. Duplicate-name check + save in the service
        var (success, message) = await _menuService.UpdateCategoryAsync(Input);
        if (!success)
        {
            ModelState.AddModelError("Input.Name", message);
            return Page();
        }

        TempData["SuccessMessage"] = message;
        return RedirectToPage("./Index", new { tab = "categories" });
    }
}
