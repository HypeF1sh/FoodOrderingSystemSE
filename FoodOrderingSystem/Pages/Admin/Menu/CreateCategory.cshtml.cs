// PB06 (Jordan) - add a menu category
using FoodOrderingSystem.Data;
using FoodOrderingSystem.Services;
using FoodOrderingSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FoodOrderingSystem.Pages.Admin.Menu;

[Authorize(Roles = DbSeeder.AdminRole)]
public class CreateCategoryModel : PageModel
{
    private readonly IMenuService _menuService;

    public CreateCategoryModel(IMenuService menuService)
    {
        _menuService = menuService;
    }

    [BindProperty]
    public MenuCategoryInput Input { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // 1. Field checks from the data annotations
        if (!ModelState.IsValid) return Page();

        // 2. Duplicate-name check + save in the service
        var (success, message) = await _menuService.CreateCategoryAsync(Input);
        if (!success)
        {
            ModelState.AddModelError("Input.Name", message);
            return Page();
        }

        // 3. Back to the Categories tab
        TempData["SuccessMessage"] = message;
        return RedirectToPage("./Index", new { tab = "categories" });
    }
}
