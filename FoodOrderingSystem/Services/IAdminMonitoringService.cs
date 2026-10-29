using FoodOrderingSystem.ViewModels;   // <-- match your project namespace

namespace FoodOrderingSystem.Services;

public interface IAdminMonitoringService
{
    /// <summary>
    /// All customers' reservations between two dates (inclusive), optionally filtered by status name.
    /// Reads through IReservationService only (cross-domain rule).
    /// </summary>
    Task<ReservationMonitorViewModel> GetReservationsAsync(DateOnly from, DateOnly to, string? status);
}
