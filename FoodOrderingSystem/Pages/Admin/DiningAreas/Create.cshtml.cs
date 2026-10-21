using System.ComponentModel.DataAnnotations;
using FoodOrderingSystem.Services;     // <-- match your project namespace
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FoodOrderingSystem.Pages.Admin.DiningAreas;

public class CreateModel : PageModel
{
    private readonly IDiningAreaService _service;

    public CreateModel(IDiningAreaService service)
    {
        _service = service;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [TempData] public string? SuccessMessage { get; set; }

    public class InputModel
    {
        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }
    }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var (success, message) = await _service.CreateAreaAsync(Input.Name, Input.Description);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, message);
            return Page();
        }

        SuccessMessage = message;
        return RedirectToPage("Index");
    }
}
