// PB07 (Carynn) - restaurant settings (interface)
using FoodOrderingSystem.Models;   // <-- match your project namespace

namespace FoodOrderingSystem.Services;

public interface IRestaurantSettingService
{
    Task<List<RestaurantSetting>> GetAllAsync();                      // ordered by Key
    Task<(bool Success, string Message)> UpdateValuesAsync(Dictionary<int, string> valuesById);
    Task<(bool Success, string Message)> AddAsync(string key, string value);
}
