// PB05 (Carynn) - weekly opening hours (interface)
using FoodOrderingSystem.ViewModels;   // <-- match your project namespace

namespace FoodOrderingSystem.Services;

public interface IOperatingHoursService
{
    /// <summary>Always returns 7 items (Sunday=0 to Saturday=6).</summary>
    Task<List<OperatingHoursDayViewModel>> GetWeekAsync();

    Task<(bool Success, string Message)> UpdateWeekAsync(IEnumerable<OperatingHoursDayViewModel> days);
}
