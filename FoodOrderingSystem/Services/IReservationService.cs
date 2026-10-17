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
}