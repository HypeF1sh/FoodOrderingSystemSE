using System.Text.Json;
using FoodOrderingSystem.Data;
using FoodOrderingSystem.Services;
using FoodOrderingSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FoodOrderingSystem.Pages.Admin.Dashboard;

// Admin dashboard: sales, bookings and popular dishes for a chosen date range (PB22)
[Authorize(Roles = DbSeeder.AdminRole)]
public class IndexModel : PageModel
{
    // Default range when the page opens: the last 7 days, today included (Dish sales uses it too)
    public const int DefaultRangeDays = 7;

    private readonly IReportService _reportService;
    private readonly IExportService _exportService;   // PB24

    public IndexModel(IReportService reportService, IExportService exportService)
    {
        _reportService = reportService;
        _exportService = exportService;
    }

    // Bound from the query string so a report can be bookmarked, e.g. ?from=2026-10-01&to=2026-10-31&grouping=Week
    [BindProperty(SupportsGet = true)]
    public DateOnly? From { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateOnly? To { get; set; }

    // Null = let the service pick one that suits the length of the range
    [BindProperty(SupportsGet = true)]
    public ReportGrouping? Grouping { get; set; }

    public DateOnly Today { get; } = DateOnly.FromDateTime(DateTime.Now);
    public DateOnly SelectedFrom => From ?? Today.AddDays(1 - DefaultRangeDays);
    public DateOnly SelectedTo => To ?? Today;

    public DashboardViewModel? Dashboard { get; set; }

    // Chart data for admin-dashboard.js, as JSON inside a <script type="application/json"> block.
    // System.Text.Json escapes < > & by default, so a dish name cannot break out of the script tag.
    public string ChartDataJson { get; set; } = "{}";

    public async Task OnGetAsync()
    {
        var grouping = Grouping ?? _reportService.SuggestGrouping(SelectedFrom, SelectedTo);

        var (success, message, dashboard) = await _reportService.GetDashboardAsync(SelectedFrom, SelectedTo, grouping);
        if (!success || dashboard == null)
        {
            // A bad range is a filter error, so it shows in the filter form's validation summary
            ModelState.AddModelError(string.Empty, message);
            return;
        }

        Dashboard = dashboard;
        ChartDataJson = JsonSerializer.Serialize(new
        {
            sales = ToSeries(dashboard.SalesByPeriod),
            bookings = ToSeries(dashboard.BookingsByPeriod),
            dishes = new
            {
                labels = dashboard.PopularItems.Select(p => p.Name),
                values = dashboard.PopularItems.Select(p => p.QuantitySold)
            }
        });
    }

    // PB24: /Admin/Dashboard?handler=Export&from=...&to=...&grouping=...
    // Same filters and the same report service as the screen, so the CSV has the same numbers.
    public async Task<IActionResult> OnGetExportAsync()
    {
        var grouping = Grouping ?? _reportService.SuggestGrouping(SelectedFrom, SelectedTo);

        var (success, message, dashboard) = await _reportService.GetDashboardAsync(SelectedFrom, SelectedTo, grouping);
        if (!success || dashboard == null)
        {
            ModelState.AddModelError(string.Empty, message);
            return Page();      // show the same filter error as the screen; no file is downloaded
        }

        var bytes = _exportService.BuildDashboardCsv(dashboard, SelectedFrom, SelectedTo);
        return File(bytes, "text/csv", $"dashboard-{DateTime.Today:yyyyMMdd}.csv");
    }

    private static object ToSeries(List<ReportPointViewModel> points) => new
    {
        labels = points.Select(p => p.Label),
        values = points.Select(p => p.Value)
    };
}
