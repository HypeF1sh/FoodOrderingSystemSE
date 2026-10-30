using FoodOrderingSystem.Services;
using FoodOrderingSystem.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FoodOrderingSystem.Pages.Admin.Reports;

public class CustomerActivityModel : PageModel
{
    private readonly ICustomerActivityService _activityService;

    public CustomerActivityModel(ICustomerActivityService monitoringService)
    {
        _activityService = monitoringService;
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

    public async Task OnGetAsync()
    {
        // Default window: the last 30 days
        To ??= DateOnly.FromDateTime(DateTime.Today);
        From ??= To.Value.AddDays(-29);
        Sort = string.Equals(Sort, "spend", StringComparison.OrdinalIgnoreCase) ? "spend" : "active";

        if (To < From)
        {
            ModelState.AddModelError(string.Empty, "The end date must be on or after the start date.");
            return;
        }

        var days = To.Value.DayNumber - From.Value.DayNumber + 1;
        if (days > CustomerActivityService.MaxRangeDays)
        {
            ModelState.AddModelError(string.Empty,
                $"Please choose a range of {CustomerActivityService.MaxRangeDays} days or fewer.");
            return;
        }

        Customers = await _activityService.GetCustomerActivityAsync(From.Value, To.Value, Sort);
    }
}
