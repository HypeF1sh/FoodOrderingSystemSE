using System.ComponentModel.DataAnnotations;
using FoodOrderingSystem.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FoodOrderingSystem.Pages.Admin.Announcements;

public class EditModel : PageModel
{
    private readonly IAnnouncementService _service;

    public EditModel(IAnnouncementService service)
    {
        _service = service;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [TempData] public string? SuccessMessage { get; set; }

    public class InputModel
    {
        public int Id { get; set; }

        [Required, StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required, StringLength(2000)]
        public string Body { get; set; } = string.Empty;

        [Display(Name = "Active")]
        public bool IsActive { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var item = await _service.GetAsync(id);
        if (item == null) return NotFound();

        Input = new InputModel { Id = item.AnnouncementId, Title = item.Title, Body = item.Body, IsActive = item.IsActive };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var (success, message) = await _service.UpdateAsync(Input.Id, Input.Title, Input.Body, Input.IsActive);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, message);
            return Page();
        }

        SuccessMessage = message;
        return RedirectToPage("Index");
    }
}
