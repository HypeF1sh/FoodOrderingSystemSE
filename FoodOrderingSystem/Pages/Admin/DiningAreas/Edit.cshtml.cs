// PB05 (Carynn) - edit a dining area
using System.ComponentModel.DataAnnotations;
using FoodOrderingSystem.Services;     // <-- match your project namespace
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FoodOrderingSystem.Pages.Admin.DiningAreas;

public class EditModel : PageModel
{
    private readonly IDiningAreaService _service;

    public EditModel(IDiningAreaService service)
    {
        _service = service;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [TempData] public string? SuccessMessage { get; set; }

    public class InputModel
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }
    }

    // GET: load what the page shows
    public async Task<IActionResult> OnGetAsync(int id)
    {
        var area = await _service.GetAreaAsync(id);
        if (area == null) return NotFound();

        Input = new InputModel { Id = area.DiningAreaId, Name = area.Name, Description = area.Description };
        return Page();
    }

    // POST: save the form
    public async Task<IActionResult> OnPostAsync()
    {
        // Field checks from the data annotations
        if (!ModelState.IsValid) return Page();

        // The service checks the business rules and saves
        var (success, message) = await _service.UpdateAreaAsync(Input.Id, Input.Name, Input.Description);
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
