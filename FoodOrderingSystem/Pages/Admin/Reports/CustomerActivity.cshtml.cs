// PB23/PB24 (Carynn) - customer activity report, with CSV export
using FoodOrderingSystem.Services;
using FoodOrderingSystem.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FoodOrderingSystem.Pages.Admin.Reports;

public class CustomerActivityModel : PageModel
{
    private readonly ICustomerActivityService _activityService;
    private readonly IExportService _exportService;

    public CustomerActivityModel(ICustomerActivityService activityService, IExportService exportService)
    {
        _activityService = activityService;
        _exportService = exportService;
    }

    [BindProperty(SupportsGet = true)] public DateOnly? From { get; set; }
    [BindProperty(SupportsGet = true)] public DateOnly? To { get; set; }
    [BindProperty(SupportsGet = true)] public string? Sort { get; set; }

    public List<CustomerActivityRowViewModel> Customers { get; set; } = new();

    public List<SelectListItem> SortOptions { get; } = new()
    {
        new("Most active (reservations + orders)", "active"),
        new("Highest spend", "spend")
    };

    // GET: load what the page shows
    public async Task OnGetAsync()
    {
        ApplyDefaults();
        if (!RangeIsValid()) return;

        Customers = await _activityService.GetCustomerActivityAsync(From!.Value, To!.Value, Sort);
    }

    // PB24: /Admin/Reports/CustomerActivity?handler=Export&From=...&To=...&Sort=...
    // Uses the same filters as the screen, so the CSV has the same numbers.
    public async Task<IActionResult> OnGetExportAsync()
    {
        ApplyDefaults();
        if (!RangeIsValid()) return Page();     // show the same error as the screen

        var rows = await _activityService.GetCustomerActivityAsync(From!.Value, To!.Value, Sort);
        var bytes = _exportService.BuildCustomerActivityCsv(rows);

        return File(bytes, "text/csv", $"customer-activity-{DateTime.Today:yyyyMMdd}.csv");
    }

    private void ApplyDefaults()
    {
        // Default window: the last 30 days
        To ??= DateOnly.FromDateTime(DateTime.Today);
        From ??= To.Value.AddDays(-29);
        Sort = string.Equals(Sort, "spend", StringComparison.OrdinalIgnoreCase) ? "spend" : "active";
    }

    private bool RangeIsValid()
    {
        if (To < From)
        {
            ModelState.AddModelError(string.Empty, "The end date must be on or after the start date.");
            return false;
        }

        var days = To!.Value.DayNumber - From!.Value.DayNumber + 1;
        if (days > CustomerActivityService.MaxRangeDays)
        {
            ModelState.AddModelError(string.Empty,
                $"Please choose a range of {CustomerActivityService.MaxRangeDays} days or fewer.");
            return false;
        }

        return true;
    }
}
