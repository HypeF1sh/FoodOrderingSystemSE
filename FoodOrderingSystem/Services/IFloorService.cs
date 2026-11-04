using FoodOrderingSystem.ViewModels;

namespace FoodOrderingSystem.Services;

// Staff Floor, Needs-you and Today (spec: docs/superpowers/specs/2026-11-01-staff-floor-design.md)
public interface IFloorService
{
    // Tiles for one dining area (the first area when diningAreaId is null or unknown) plus the Needs-you list
    Task<FloorViewModel> GetFloorAsync(int? diningAreaId);

    Task<StaffTodayViewModel> GetTodayAsync();

    // Refuses while an order is still cooking or unpaid; otherwise completes paid, served orders, then the booking
    // reservationId is null for a walk-in party (table QR orders, no booking)
    Task<(bool Success, string Message)> CompleteTableAsync(int restaurantTableId, int? reservationId);
}
