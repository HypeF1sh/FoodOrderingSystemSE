using FoodOrderingSystem.Data;     // <-- match your project namespace
using FoodOrderingSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace FoodOrderingSystem.Services;

public class RestaurantSettingService : IRestaurantSettingService
{
    private readonly ApplicationDbContext _db;

    public RestaurantSettingService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<RestaurantSetting>> GetAllAsync() =>
        await _db.RestaurantSettings.AsNoTracking().OrderBy(s => s.Key).ToListAsync();

    public async Task<(bool Success, string Message)> UpdateValuesAsync(Dictionary<int, string> valuesById)
    {
        var ids = valuesById.Keys.ToList();
        var settings = await _db.RestaurantSettings.Where(s => ids.Contains(s.RestaurantSettingId)).ToListAsync();

        foreach (var setting in settings)
        {
            var value = (valuesById[setting.RestaurantSettingId] ?? string.Empty).Trim();
            if (value.Length == 0) return (false, $"\"{setting.Key}\" needs a value.");
            if (value.Length > 500) return (false, $"\"{setting.Key}\" is too long (max 500 characters).");
            setting.Value = value;
        }

        await _db.SaveChangesAsync();
        return (true, "Settings were saved.");
    }

    public async Task<(bool Success, string Message)> AddAsync(string key, string value)
    {
        key = (key ?? string.Empty).Trim();
        value = (value ?? string.Empty).Trim();

        if (key.Length == 0) return (false, "Key is required.");
        if (key.Length > 100) return (false, "Key is too long (max 100 characters).");
        if (value.Length == 0) return (false, "Value is required.");
        if (value.Length > 500) return (false, "Value is too long (max 500 characters).");

        if (await _db.RestaurantSettings.AnyAsync(s => s.Key == key))
            return (false, $"A setting named \"{key}\" already exists.");

        _db.RestaurantSettings.Add(new RestaurantSetting { Key = key, Value = value });
        await _db.SaveChangesAsync();
        return (true, $"Setting \"{key}\" was added.");
    }
}
