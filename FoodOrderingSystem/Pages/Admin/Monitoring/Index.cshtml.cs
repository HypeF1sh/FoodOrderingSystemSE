using FoodOrderingSystem.Models.Enums;     // <-- match your project namespace
using FoodOrderingSystem.Services;
using FoodOrderingSystem.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FoodOrderingSystem.Pages.Admin.Monitoring;

public class IndexModel : PageModel
{
    private readonly IAdminMonitoringService _monitoringService;

    public IndexModel(IAdminMonitoringService monitoringService)
    {
        _monitoringService = monitoringService;
    }

    // Filters come from the query string, so the page can be bookmarked and refreshed
    [BindProperty(SupportsGet = true)] public string? Tab { get; set; }
    [BindProperty(SupportsGet = true)] public DateOnly? From { get; set; }
    [BindProperty(SupportsGet = true)] public DateOnly? To { get; set; }
    [BindProperty(SupportsGet = true)] public string? Status { get; set; }

    public ReservationMonitorViewModel Reservations { get; set; } = new();
    public List<SelectListItem> StatusOptions { get; set; } = new();

    public async Task OnGetAsync()
    {
        // Only three valid tabs; anything else falls back to reservations
        Tab = Tab?.ToLowerInvariant() is "orders" or "payments" ? Tab.ToLowerInvariant() : "reservations";

        // Default window: one week back to one week ahead
        From ??= DateOnly.FromDateTime(DateTime.Today.AddDays(-7));
        To ??= DateOnly.FromDateTime(DateTime.Today.AddDays(7));

        if (To < From)
        {
            ModelState.AddModelError(string.Empty, "The end date must be on or after the start date.");
            return;
        }

        var days = To.Value.DayNumber - From.Value.DayNumber + 1;
        if (days > AdminMonitoringService.MaxRangeDays)
        {
            ModelState.AddModelError(string.Empty,
                $"Please choose a range of {AdminMonitoringService.MaxRangeDays} days or fewer.");
            return;
        }

        if (Tab == "reservations")
        {
            StatusOptions = new List<SelectListItem> { new("All statuses", "") };
            StatusOptions.AddRange(Enum.GetNames<ReservationStatus>().Select(n => new SelectListItem(n, n)));

            Reservations = await _monitoringService.GetReservationsAsync(From.Value, To.Value, Status);
        }

        // Orders and Payments tabs: added once Jordan's GetOrdersAsync / GetPaymentsAsync are merged
    }
}
