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

    public async Task OnGetAsync()
    {
        Days = await _service.GetWeekAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var (success, message) = await _service.UpdateWeekAsync(Days);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, message);
            return Page();
        }

        SuccessMessage = message;
        return RedirectToPage();
    }
}
