using FoodOrderingSystem.ViewModels;

namespace FoodOrderingSystem.Services;

public interface IOccupancyService
{
    /// <summary>
    /// Suggests Day grouping for ranges up to 31 days, or Week grouping for longer ranges.
    /// </summary>
    OccupancyGrouping SuggestGrouping(DateOnly from, DateOnly to);

    /// <summary>
    /// PB25: Calculates peak hours, table utilisation %, and booking/guest trends for [from, to].
    /// Excludes Cancelled and NoShow reservations. Max range is 366 days.
    /// </summary>
    Task<(bool Success, string Message, OccupancyReportViewModel? Report)> GetOccupancyReportAsync(
        DateOnly from,
        DateOnly to,
        OccupancyGrouping? grouping = null);
}