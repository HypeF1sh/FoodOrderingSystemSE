// PB05 (Carynn) - edit the weekly opening hours
using FoodOrderingSystem.Services;      // <-- match your project namespace
using FoodOrderingSystem.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FoodOrderingSystem.Pages.Admin.OperatingHours;

public class IndexModel : PageModel
{
    private readonly IOperatingHoursService _service;

    public IndexModel(IOperatingHoursService service)
    {
        _service = service;
    }

    [BindProperty]
    public List<OperatingHoursDayViewModel> Days { get; set; } = new();

    [TempData] public string? SuccessMessage { get; set; }

    // GET: load what the page shows
    public async Task OnGetAsync()
    {
        Days = await _service.GetWeekAsync();
    }

    // POST: save the form
    public async Task<IActionResult> OnPostAsync()
    {
        // Field checks from the data annotations
        if (!ModelState.IsValid) return Page();

        // The service checks the business rules and saves
        var (success, message) = await _service.UpdateWeekAsync(Days);
        // Rule failed: show the message on the form
        if (!success)
        {
            ModelState.AddModelError(string.Empty, message);
            return Page();
        }

        // Success: message for the next page, then redirect (Post/Redirect/Get)
        SuccessMessage = message;
        return RedirectToPage();
    }
}
