using FoodOrderingSystem.ViewModels;

namespace FoodOrderingSystem.Services;

// Reports and the admin dashboard (PB22). Read-only: never changes any data.
public interface IReportService
{
    /// <summary>
    /// Sales, bookings and popular dishes for 'from'..'to' (both days included), with the charts
    /// grouped by day, week or month. Fails with a message if 'from' is after 'to' or the range is too long.
    /// </summary>
    Task<(bool Success, string Message, DashboardViewModel? Dashboard)> GetDashboardAsync(
        DateOnly from, DateOnly to, ReportGrouping grouping);

    /// <summary>Sensible grouping for a range: Day up to 31 days, Week up to 92, otherwise Month.</summary>
    ReportGrouping SuggestGrouping(DateOnly from, DateOnly to);
}
