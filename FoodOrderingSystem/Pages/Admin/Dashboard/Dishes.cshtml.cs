using FoodOrderingSystem.Data;
using FoodOrderingSystem.Services;
using FoodOrderingSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FoodOrderingSystem.Pages.Admin.Dashboard;

// Dish sales report: every dish with quantity sold and item sales for a date range (PB22).
// Opened from the dashboard's "Popular dishes" card, which only shows the top 5.
[Authorize(Roles = DbSeeder.AdminRole)]
public class DishesModel : PageModel
{
    private readonly IReportService _reportService;

    public DishesModel(IReportService reportService)
    {
        _reportService = reportService;
    }

    // Bound from the query string, same as the dashboard, so the dashboard can link here with its range
    [BindProperty(SupportsGet = true)]
    public DateOnly? From { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateOnly? To { get; set; }

    public DateOnly Today { get; } = DateOnly.FromDateTime(DateTime.Now);
    public DateOnly SelectedFrom => From ?? Today.AddDays(1 - IndexModel.DefaultRangeDays);
    public DateOnly SelectedTo => To ?? Today;

    public DishSalesViewModel? Report { get; set; }

    public async Task OnGetAsync()
    {
        var (success, message, report) = await _reportService.GetDishSalesAsync(SelectedFrom, SelectedTo);
        if (!success || report == null)
        {
            // A bad range is a filter error, so it shows in the filter form's validation summary
            ModelState.AddModelError(string.Empty, message);
            return;
        }

        Report = report;
    }
}
