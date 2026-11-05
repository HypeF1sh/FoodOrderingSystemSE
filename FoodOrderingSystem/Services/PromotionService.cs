using FoodOrderingSystem.Data;
using FoodOrderingSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace FoodOrderingSystem.Services;

public class PromotionService : IPromotionService
{
    private readonly ApplicationDbContext _db;

    public PromotionService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<Promotion>> GetAllAsync() =>
        await _db.Promotions.AsNoTracking().OrderByDescending(p => p.StartDate).ToListAsync();

    public async Task<Promotion?> GetAsync(int id) =>
        await _db.Promotions.AsNoTracking().FirstOrDefaultAsync(p => p.PromotionId == id);

    public async Task<(bool Success, string Message)> CreateAsync(
        string title, string? description, decimal discountPercent, DateTime startDate, DateTime endDate, bool isActive)
    {
        var error = Validate(title, discountPercent, startDate, endDate);
        if (error != null) return (false, error);

        _db.Promotions.Add(new Promotion
        {
            Title = title.Trim(),
            Description = description?.Trim() ?? string.Empty,
            DiscountPercent = discountPercent,
            StartDate = startDate.Date,
            EndDate = endDate.Date,
            IsActive = isActive
        });
        await _db.SaveChangesAsync();
        return (true, $"Promotion \"{title.Trim()}\" was created.");
    }

    public async Task<(bool Success, string Message)> UpdateAsync(
        int id, string title, string? description, decimal discountPercent, DateTime startDate, DateTime endDate, bool isActive)
    {
        var promo = await _db.Promotions.FindAsync(id);
        if (promo == null) return (false, "Promotion not found.");

        var error = Validate(title, discountPercent, startDate, endDate);
        if (error != null) return (false, error);

        promo.Title = title.Trim();
        promo.Description = description?.Trim() ?? string.Empty;
        promo.DiscountPercent = discountPercent;
        promo.StartDate = startDate.Date;
        promo.EndDate = endDate.Date;
        promo.IsActive = isActive;
        await _db.SaveChangesAsync();
        return (true, $"Promotion \"{promo.Title}\" was updated.");
    }

    public async Task<(bool Success, string Message)> ToggleActiveAsync(int id)
    {
        var promo = await _db.Promotions.FindAsync(id);
        if (promo == null) return (false, "Promotion not found.");

        promo.IsActive = !promo.IsActive;
        await _db.SaveChangesAsync();
        return (true, $"Promotion \"{promo.Title}\" is now {(promo.IsActive ? "active" : "inactive")}.");
    }

    public async Task<(bool Success, string Message)> DeleteAsync(int id)
    {
        var promo = await _db.Promotions.FindAsync(id);
        if (promo == null) return (false, "Promotion not found.");

        _db.Promotions.Remove(promo);
        await _db.SaveChangesAsync();
        return (true, $"Promotion \"{promo.Title}\" was deleted.");
    }

    public async Task<List<Promotion>> GetActiveAsync()
    {
        var today = DateTime.Today;   // dates are saved without a time, so the end day is included

        return await _db.Promotions
            .AsNoTracking()
            .Where(p => p.IsActive && p.StartDate <= today && p.EndDate >= today)
            .OrderByDescending(p => p.DiscountPercent)
            .ThenBy(p => p.EndDate)
            .ToListAsync();
    }

    private static string? Validate(string title, decimal discountPercent, DateTime startDate, DateTime endDate)
    {
        if (string.IsNullOrWhiteSpace(title)) return "Title is required.";
        if (discountPercent < 1 || discountPercent > 90) return "Discount must be between 1 and 90 percent.";
        if (endDate.Date < startDate.Date) return "End date must be on or after the start date.";
        return null;
    }
}
