// PB05 (Carynn) - dining areas and tables (interface)
using FoodOrderingSystem.Models;   // <-- match your project namespace

namespace FoodOrderingSystem.Services;

public interface IDiningAreaService
{
    // ---- Dining areas ----
    Task<List<DiningArea>> GetAreasAsync();                 // includes RestaurantTables
    Task<DiningArea?> GetAreaAsync(int id);
    Task<(bool Success, string Message)> CreateAreaAsync(string name, string? description);
    Task<(bool Success, string Message)> UpdateAreaAsync(int id, string name, string? description);
    Task<(bool Success, string Message)> DeleteAreaAsync(int id);   // blocked if area still has tables

    // ---- Tables ----
    Task<List<RestaurantTable>> GetTablesAsync();           // includes DiningArea, ordered by area then number
    Task<RestaurantTable?> GetTableAsync(int id);
    Task<(bool Success, string Message)> CreateTableAsync(int diningAreaId, string tableNumber, int capacity, bool isActive);
    Task<(bool Success, string Message)> UpdateTableAsync(int id, int diningAreaId, string tableNumber, int capacity, bool isActive);
    Task<(bool Success, string Message)> DeleteTableAsync(int id);  // deactivates instead if it has reservations
}
