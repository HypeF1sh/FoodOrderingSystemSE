// PB25 (Wallace) - table occupancy analytics: peak hours, utilisation
using System.Text.Json;
using FoodOrderingSystem.Data;
using FoodOrderingSystem.Services;
using FoodOrderingSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FoodOrderingSystem.Pages.Admin.Occupancy;

// PB25: Table occupancy analytics showing peak hours, table utilisation % and trends
[Authorize(Roles = DbSeeder.AdminRole)]
public class IndexModel : PageModel
{
    public const int DefaultRangeDays = 7;

    private readonly IOccupancyService _occupancyService;

    public IndexModel(IOccupancyService occupancyService)
    {
        _occupancyService = occupancyService;
    }

    [BindProperty(SupportsGet = true)]
    public DateOnly? From { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateOnly? To { get; set; }

    [BindProperty(SupportsGet = true)]
    public OccupancyGrouping? Grouping { get; set; }

    public DateOnly Today { get; } = DateOnly.FromDateTime(DateTime.Now);
    public DateOnly SelectedFrom => From ?? Today.AddDays(1 - DefaultRangeDays);
    public DateOnly SelectedTo => To ?? Today;

    public OccupancyReportViewModel? Report { get; set; }
    public string ChartDataJson { get; set; } = "{}";

    // GET: build the report for the chosen range; errors (bad range) shown on the page
    public async Task OnGetAsync()
    {
        var (success, message, report) = await _occupancyService.GetOccupancyReportAsync(
            SelectedFrom, SelectedTo, Grouping);

        if (!success || report == null)
        {
            ModelState.AddModelError(string.Empty, message);
            return;
        }

        Report = report;
        ChartDataJson = JsonSerializer.Serialize(new
        {
            peakHours = new
            {
                labels = report.HourlyPeaks.Select(h => h.Label),
                bookings = report.HourlyPeaks.Select(h => h.BookingCount),
                guests = report.HourlyPeaks.Select(h => h.GuestCount)
            },
            tables = new
            {
                labels = report.TableUtilizations.Select(t => $"Table {t.TableNumber} ({t.DiningAreaName})"),
                utilization = report.TableUtilizations.Select(t => t.UtilizationPercent),
                bookings = report.TableUtilizations.Select(t => t.BookingCount)
            },
            trend = new
            {
                labels = report.TrendPoints.Select(p => p.Label),
                bookings = report.TrendPoints.Select(p => p.BookingCount),
                guests = report.TrendPoints.Select(p => p.GuestCount)
            }
        });
    }
}