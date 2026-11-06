// PB07 (Carynn) - add an announcement
using System.ComponentModel.DataAnnotations;
using FoodOrderingSystem.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FoodOrderingSystem.Pages.Admin.Announcements;

public class CreateModel : PageModel
{
    private readonly IAnnouncementService _service;

    public CreateModel(IAnnouncementService service)
    {
        _service = service;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [TempData] public string? SuccessMessage { get; set; }

    public class InputModel
    {
        [Required, StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required, StringLength(2000)]
        public string Body { get; set; } = string.Empty;

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;
    }

    public void OnGet() { }

    // POST: save the form
    public async Task<IActionResult> OnPostAsync()
    {
        // Field checks from the data annotations
        if (!ModelState.IsValid) return Page();

        // The service checks the business rules and saves
        var (success, message) = await _service.CreateAsync(Input.Title, Input.Body, Input.IsActive);
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
