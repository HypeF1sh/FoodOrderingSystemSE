using FoodOrderingSystem.Models;   // <-- match your project namespace

namespace FoodOrderingSystem.Services;

public interface IAnnouncementService
{
    Task<List<Announcement>> GetAllAsync();
    Task<List<Announcement>> GetActiveAsync();     // for the home page partial
    Task<Announcement?> GetAsync(int id);
    Task<(bool Success, string Message)> CreateAsync(string title, string body, bool isActive);
    Task<(bool Success, string Message)> UpdateAsync(int id, string title, string body, bool isActive);
    Task<(bool Success, string Message)> ToggleActiveAsync(int id);
    Task<(bool Success, string Message)> DeleteAsync(int id);
}
