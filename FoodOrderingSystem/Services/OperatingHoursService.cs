// PB05 (Carynn) - weekly opening hours: read and save
using FoodOrderingSystem.Data;         // <-- match your project namespace
using FoodOrderingSystem.Models;
using FoodOrderingSystem.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace FoodOrderingSystem.Services;

public class OperatingHoursService : IOperatingHoursService
{
    private readonly ApplicationDbContext _db;

    public OperatingHoursService(ApplicationDbContext db)
    {
        _db = db;
    }

    // PB05: always returns 7 days (Sun-Sat); used by bookings, home page and chatbot
    public async Task<List<OperatingHoursDayViewModel>> GetWeekAsync()
    {
        var rows = await _db.OperatingHours.AsNoTracking().ToListAsync();
        var week = new List<OperatingHoursDayViewModel>();

        for (var day = 0; day <= 6; day++)
        {
            var row = rows.FirstOrDefault(r => r.DayOfWeek == day);
            week.Add(new OperatingHoursDayViewModel
            {
                DayOfWeek = day,
                // default for a day that has no row yet
                OpenTime = row?.OpenTime ?? new TimeOnly(9, 0),
                CloseTime = row?.CloseTime ?? new TimeOnly(22, 0),
                IsClosed = row?.IsClosed ?? false
            });
        }
        return week;
    }

    // PB05: save all 7 days at once, after validating every day
    public async Task<(bool Success, string Message)> UpdateWeekAsync(IEnumerable<OperatingHoursDayViewModel> days)
    {
        var list = days.ToList();
        if (list.Count != 7 || list.Select(d => d.DayOfWeek).Distinct().Count() != 7)
            return (false, "All 7 days are required.");

        // Validate everything first: Close must be after Open unless the day is closed
        foreach (var d in list)
        {
            if (d.DayOfWeek is < 0 or > 6) return (false, "Invalid day of week.");
            if (!d.IsClosed && d.CloseTime <= d.OpenTime)
                return (false, $"{d.DayName}: closing time must be after opening time (or mark the day as closed).");
        }

        // Update existing rows, create missing ones
        var existing = await _db.OperatingHours.ToListAsync();

        foreach (var d in list)
        {
            var row = existing.FirstOrDefault(r => r.DayOfWeek == d.DayOfWeek);
            if (row == null)
            {
                row = new OperatingHours { DayOfWeek = d.DayOfWeek };
                _db.OperatingHours.Add(row);
            }
            row.OpenTime = d.OpenTime;
            row.CloseTime = d.CloseTime;
            row.IsClosed = d.IsClosed;
        }

        await _db.SaveChangesAsync();
        return (true, "Operating hours were saved.");
    }
}
