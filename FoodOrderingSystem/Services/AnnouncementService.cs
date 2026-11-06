// PB07 (Carynn) - announcements: CRUD and active list
using FoodOrderingSystem.Data;     // <-- match your project namespace
using FoodOrderingSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace FoodOrderingSystem.Services;

public class AnnouncementService : IAnnouncementService
{
    private readonly ApplicationDbContext _db;

    public AnnouncementService(ApplicationDbContext db)
    {
        _db = db;
    }

    // PB07: all announcements, newest first
    public async Task<List<Announcement>> GetAllAsync() =>
        await _db.Announcements.AsNoTracking().OrderByDescending(a => a.PublishedAt).ToListAsync();

    // PB07: only the switched-on ones, for customers
    public async Task<List<Announcement>> GetActiveAsync() =>
        await _db.Announcements.AsNoTracking()
            .Where(a => a.IsActive)
            .OrderByDescending(a => a.PublishedAt)
            .ToListAsync();

    public async Task<Announcement?> GetAsync(int id) =>
        await _db.Announcements.AsNoTracking().FirstOrDefaultAsync(a => a.AnnouncementId == id);

    // PB07: add one; title and body required
    public async Task<(bool Success, string Message)> CreateAsync(string title, string body, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(title)) return (false, "Title is required.");
        if (string.IsNullOrWhiteSpace(body)) return (false, "Body is required.");

        _db.Announcements.Add(new Announcement
        {
            Title = title.Trim(),
            Body = body.Trim(),
            IsActive = isActive,
            PublishedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        return (true, $"Announcement \"{title.Trim()}\" was created.");
    }

    // PB07: edit one
    public async Task<(bool Success, string Message)> UpdateAsync(int id, string title, string body, bool isActive)
    {
        var item = await _db.Announcements.FindAsync(id);
        if (item == null) return (false, "Announcement not found.");
        if (string.IsNullOrWhiteSpace(title)) return (false, "Title is required.");
        if (string.IsNullOrWhiteSpace(body)) return (false, "Body is required.");

        item.Title = title.Trim();
        item.Body = body.Trim();
        item.IsActive = isActive;
        await _db.SaveChangesAsync();
        return (true, $"Announcement \"{item.Title}\" was updated.");
    }

    // PB07: show or hide without deleting
    public async Task<(bool Success, string Message)> ToggleActiveAsync(int id)
    {
        var item = await _db.Announcements.FindAsync(id);
        if (item == null) return (false, "Announcement not found.");

        item.IsActive = !item.IsActive;
        await _db.SaveChangesAsync();
        return (true, $"Announcement \"{item.Title}\" is now {(item.IsActive ? "active" : "inactive")}.");
    }

    // PB07: delete one
    public async Task<(bool Success, string Message)> DeleteAsync(int id)
    {
        var item = await _db.Announcements.FindAsync(id);
        if (item == null) return (false, "Announcement not found.");

        _db.Announcements.Remove(item);
        await _db.SaveChangesAsync();
        return (true, $"Announcement \"{item.Title}\" was deleted.");
    }
}
