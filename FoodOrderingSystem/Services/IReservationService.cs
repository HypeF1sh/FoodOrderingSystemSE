using FoodOrderingSystem.ViewModels;

namespace FoodOrderingSystem.Services;

public interface IReservationService
{
    Task<List<DiningAreaOptionViewModel>> GetDiningAreasAsync();

    Task<(bool IsOpen, string ErrorMessage)> ValidateOperatingHoursAsync(DateOnly date, TimeOnly startTime);


    /// PB09: Returns active tables that fit the party size and have no overlapping booking
    /// during the 90-minute dining slot + 15-minute turnover buffer.
    Task<List<AvailableTableViewModel>> GetAvailableTablesAsync(
        DateOnly date,
        TimeOnly startTime,
        int partySize,
        int? diningAreaId = null,
        int? excludeReservationId = null);

    /// <summary>
    /// PB10: Creates a reservation after re-validating operating hours, customer overlap, and table availability.
    /// </summary>
    Task<(bool Success, string Message, int? ReservationId)> CreateReservationAsync(
        string customerId,
        int restaurantTableId,
        DateOnly date,
        TimeOnly startTime,
        int partySize,
        string? specialRequests);

    /// <summary>
    /// PB10: Retrieves booking details for the confirmation page.
    /// </summary>
    Task<ReservationDetailViewModel?> GetReservationByIdAsync(int reservationId, string? customerId = null);
}