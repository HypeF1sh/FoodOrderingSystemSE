using FoodOrderingSystem.Data;     // <-- match your project namespace
using FoodOrderingSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace FoodOrderingSystem.Services;

public class DiningAreaService : IDiningAreaService
{
    private readonly ApplicationDbContext _db;

    public DiningAreaService(ApplicationDbContext db)
    {
        _db = db;
    }

    // ================= Dining areas =================

    public async Task<List<DiningArea>> GetAreasAsync() =>
        await _db.DiningAreas
            .Include(a => a.RestaurantTables)
            .AsNoTracking()
            .OrderBy(a => a.Name)
            .ToListAsync();

    public async Task<DiningArea?> GetAreaAsync(int id) =>
        await _db.DiningAreas.AsNoTracking().FirstOrDefaultAsync(a => a.DiningAreaId == id);

    public async Task<(bool Success, string Message)> CreateAreaAsync(string name, string? description)
    {
        name = (name ?? string.Empty).Trim();
        if (name.Length == 0) return (false, "Name is required.");

        if (await _db.DiningAreas.AnyAsync(a => a.Name == name))
            return (false, $"A dining area named \"{name}\" already exists.");

        _db.DiningAreas.Add(new DiningArea
        {
            Name = name,
            Description = description?.Trim() ?? string.Empty
        });
        await _db.SaveChangesAsync();
        return (true, $"Dining area \"{name}\" was created.");
    }

    public async Task<(bool Success, string Message)> UpdateAreaAsync(int id, string name, string? description)
    {
        name = (name ?? string.Empty).Trim();
        if (name.Length == 0) return (false, "Name is required.");

        var area = await _db.DiningAreas.FindAsync(id);
        if (area == null) return (false, "Dining area not found.");

        if (await _db.DiningAreas.AnyAsync(a => a.Name == name && a.DiningAreaId != id))
            return (false, $"A dining area named \"{name}\" already exists.");

        area.Name = name;
        area.Description = description?.Trim() ?? string.Empty;
        await _db.SaveChangesAsync();
        return (true, $"Dining area \"{name}\" was updated.");
    }

    public async Task<(bool Success, string Message)> DeleteAreaAsync(int id)
    {
        var area = await _db.DiningAreas.FindAsync(id);
        if (area == null) return (false, "Dining area not found.");

        if (await _db.RestaurantTables.AnyAsync(t => t.DiningAreaId == id))
            return (false, $"Cannot delete \"{area.Name}\": it still has tables. Move or remove its tables first.");

        _db.DiningAreas.Remove(area);
        await _db.SaveChangesAsync();
        return (true, $"Dining area \"{area.Name}\" was deleted.");
    }

    // ================= Tables =================

    public async Task<List<RestaurantTable>> GetTablesAsync() =>
        await _db.RestaurantTables
            .Include(t => t.DiningArea)
            .AsNoTracking()
            .OrderBy(t => t.DiningArea!.Name)
            .ThenBy(t => t.TableNumber)
            .ToListAsync();

    public async Task<RestaurantTable?> GetTableAsync(int id) =>
        await _db.RestaurantTables
            .Include(t => t.DiningArea)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.RestaurantTableId == id);

    public async Task<(bool Success, string Message)> CreateTableAsync(
        int diningAreaId, string tableNumber, int capacity, bool isActive)
    {
        var error = await ValidateTableAsync(null, diningAreaId, tableNumber, capacity);
        if (error != null) return (false, error);

        tableNumber = tableNumber.Trim();
        _db.RestaurantTables.Add(new RestaurantTable
        {
            DiningAreaId = diningAreaId,
            TableNumber = tableNumber,
            Capacity = capacity,
            IsActive = isActive
        });
        await _db.SaveChangesAsync();
        return (true, $"Table {tableNumber} was created.");
    }

    public async Task<(bool Success, string Message)> UpdateTableAsync(
        int id, int diningAreaId, string tableNumber, int capacity, bool isActive)
    {
        var table = await _db.RestaurantTables.FindAsync(id);
        if (table == null) return (false, "Table not found.");

        var error = await ValidateTableAsync(id, diningAreaId, tableNumber, capacity);
        if (error != null) return (false, error);

        table.DiningAreaId = diningAreaId;
        table.TableNumber = tableNumber.Trim();
        table.Capacity = capacity;
        table.IsActive = isActive;
        await _db.SaveChangesAsync();
        return (true, $"Table {table.TableNumber} was updated.");
    }

    public async Task<(bool Success, string Message)> DeleteTableAsync(int id)
    {
        var table = await _db.RestaurantTables.FindAsync(id);
        if (table == null) return (false, "Table not found.");

        // Does this table have any reservations? (uses the navigation property, no FK name needed)
        var hasReservations = await _db.RestaurantTables
            .Where(t => t.RestaurantTableId == id)
            .AnyAsync(t => t.Reservations.Any());

        if (hasReservations)
        {
            table.IsActive = false;
            await _db.SaveChangesAsync();
            return (true, $"Table {table.TableNumber} has reservations, so it was deactivated instead of deleted.");
        }

        _db.RestaurantTables.Remove(table);
        await _db.SaveChangesAsync();
        return (true, $"Table {table.TableNumber} was deleted.");
    }

    // Returns an error message, or null if valid
    private async Task<string?> ValidateTableAsync(int? existingId, int diningAreaId, string tableNumber, int capacity)
    {
        tableNumber = (tableNumber ?? string.Empty).Trim();
        if (tableNumber.Length == 0) return "Table number is required.";
        if (capacity < 1 || capacity > 20) return "Capacity must be between 1 and 20.";

        if (!await _db.DiningAreas.AnyAsync(a => a.DiningAreaId == diningAreaId))
            return "Please choose a valid dining area.";

        // TableNumber must be unique across all tables
        var duplicate = await _db.RestaurantTables.AnyAsync(t =>
            t.TableNumber == tableNumber && (existingId == null || t.RestaurantTableId != existingId));
        if (duplicate) return $"Table number \"{tableNumber}\" is already in use.";

        return null;
    }
}
