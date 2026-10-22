using FoodOrderingSystem.ViewModels;

namespace FoodOrderingSystem.Services;

public interface IReservationService
{
    Task<List<DiningAreaOptionViewModel>> GetDiningAreasAsync();

    Task<(bool IsOpen, string ErrorMessage)> ValidateOperatingHoursAsync(DateOnly date, TimeOnly startTime);

    Task<List<AvailableTableViewModel>> GetAvailableTablesAsync(
        DateOnly date,
        TimeOnly startTime,
        int partySize,
        int? diningAreaId = null,
        int? excludeReservationId = null);

    Task<(bool Success, string Message, int? ReservationId)> CreateReservationAsync(
        string customerId,
        int restaurantTableId,
        DateOnly date,
        TimeOnly startTime,
        int partySize,
        string? specialRequests);

    Task<ReservationDetailViewModel?> GetReservationByIdAsync(int reservationId, string? customerId = null);

    /// PB11: Retrieves all reservations for a customer, newest first.
    Task<List<ReservationDetailViewModel>> GetCustomerReservationsAsync(string customerId);

    /// PB11: Modifies an upcoming reservation (excludes itself from the overlap check and enforces the 2-hour cutoff).
    Task<(bool Success, string Message)> UpdateReservationAsync(
        int reservationId,
        string customerId,
        int restaurantTableId,
        DateOnly date,
        TimeOnly startTime,
        int partySize,
        string? specialRequests);

    /// PB11: Cancels an upcoming reservation (enforces the 2-hour cutoff).
    Task<(bool Success, string Message)> CancelReservationAsync(int reservationId, string customerId);
}