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

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var area = await _service.GetAreaAsync(id);
        if (area == null) return NotFound();

        Input = new InputModel { Id = area.DiningAreaId, Name = area.Name, Description = area.Description };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var (success, message) = await _service.UpdateAreaAsync(Input.Id, Input.Name, Input.Description);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, message);
            return Page();
        }

        SuccessMessage = message;
        return RedirectToPage("Index");
    }
}
