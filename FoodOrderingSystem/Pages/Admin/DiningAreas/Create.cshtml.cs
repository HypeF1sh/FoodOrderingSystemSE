// PB05 (Carynn) - add a dining area
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

    // POST: save the form
    public async Task<IActionResult> OnPostAsync()
    {
        // Field checks from the data annotations
        if (!ModelState.IsValid) return Page();

        // The service checks the business rules and saves
        var (success, message) = await _service.CreateAreaAsync(Input.Name, Input.Description);
        // Rule failed: show the message on the form
        if (!success)
        {
            ModelState.AddModelError(string.Empty, message);
            return Page();
        }

        // Success: message for the next page, then redirect (Post/Redirect/Get)
        SuccessMessage = message;
        return RedirectToPage("Index");
    }
}
