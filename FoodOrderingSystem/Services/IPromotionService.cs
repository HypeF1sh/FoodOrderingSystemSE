// PB07 (Carynn) - promotions (interface)
using FoodOrderingSystem.Models;   // <-- match your project namespace

namespace FoodOrderingSystem.Services;

public interface IPromotionService
{
    Task<List<Promotion>> GetAllAsync();
    Task<Promotion?> GetAsync(int id);
    Task<(bool Success, string Message)> CreateAsync(string title, string? description, decimal discountPercent, DateTime startDate, DateTime endDate, bool isActive);
    Task<(bool Success, string Message)> UpdateAsync(int id, string title, string? description, decimal discountPercent, DateTime startDate, DateTime endDate, bool isActive);
    Task<(bool Success, string Message)> ToggleActiveAsync(int id);
    Task<(bool Success, string Message)> DeleteAsync(int id);
    Task<List<Promotion>> GetActiveAsync();
    /// <summary>The promotion for a code like PROMO3, if it is active, valid today and within the discount cap. Otherwise null.</summary>
    Task<Promotion?> GetActiveByCodeAsync(string? code);
}
