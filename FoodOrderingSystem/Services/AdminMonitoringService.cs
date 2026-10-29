using FoodOrderingSystem.Models.Enums;   // <-- match your project namespace
using FoodOrderingSystem.ViewModels;

namespace FoodOrderingSystem.Services;

public class AdminMonitoringService : IAdminMonitoringService
{
    // The reservation service returns one day at a time, so the range is capped
    public const int MaxRangeDays = 62;

    private readonly IReservationService _reservationService;

    public AdminMonitoringService(IReservationService reservationService)
    {
        _reservationService = reservationService;
    }

    public async Task<ReservationMonitorViewModel> GetReservationsAsync(DateOnly from, DateOnly to, string? status)
    {
        if (to < from) (from, to) = (to, from);
        if (to.DayNumber - from.DayNumber + 1 > MaxRangeDays)
            to = from.AddDays(MaxRangeDays - 1);

        // Ask Wallace's service for each day in the range.
        // If a "get reservations between two dates" method is added later, replace this loop with it.
        var all = new List<ReservationDetailViewModel>();
        for (var day = from; day <= to; day = day.AddDays(1))
        {
            all.AddRange(await _reservationService.GetStaffDailyReservationsAsync(day));
        }

        var counts = Enum.GetNames<ReservationStatus>()
            .ToDictionary(
                name => name,
                name => all.Count(r => string.Equals(r.Status, name, StringComparison.OrdinalIgnoreCase)));

        // Filter by status here (not in the other service), so it works whatever format its filter expects
        var filtered = string.IsNullOrWhiteSpace(status)
            ? all
            : all.Where(r => string.Equals(r.Status, status, StringComparison.OrdinalIgnoreCase)).ToList();

        return new ReservationMonitorViewModel
        {
            Reservations = filtered.OrderBy(r => r.ReservationDate).ThenBy(r => r.StartTime).ToList(),
            TotalInRange = all.Count,
            StatusCounts = counts
        };
    }
}
