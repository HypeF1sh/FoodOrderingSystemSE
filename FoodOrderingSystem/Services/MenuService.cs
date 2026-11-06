// PB06/PB08 (Jordan) - menu: browse, admin CRUD, availability, delete checks
using FoodOrderingSystem.Data;
using FoodOrderingSystem.Models;
using FoodOrderingSystem.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace FoodOrderingSystem.Services;

public class MenuService : IMenuService
{
    private readonly ApplicationDbContext _context;
    private readonly IImageStorageService _imageStorage;

    public MenuService(ApplicationDbContext context, IImageStorageService imageStorage)
    {
        _context = context;
        _imageStorage = imageStorage;
    }

    // =====================================================================
    // Customer browsing (PB08)
    // =====================================================================

    public async Task<MenuBrowseViewModel> GetMenuForBrowseAsync(int? categoryId, string? search)
    {
        // 1. Load the active categories in menu order (inactive ones are hidden from customers)
        var categories = await _context.MenuCategories
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name)
            .ToListAsync();

        // 2. An unknown or inactive category id in the URL falls back to "All"
        if (categoryId.HasValue && !categories.Any(c => c.MenuCategoryId == categoryId))
            categoryId = null;

        // 3. Build the item query step by step; EF turns it into one SQL query with a WHERE clause
        var itemsQuery = _context.MenuItems
            .AsNoTracking()
            .Where(i => i.MenuCategory != null && i.MenuCategory.IsActive);

        if (categoryId.HasValue)
            itemsQuery = itemsQuery.Where(i => i.MenuCategoryId == categoryId);

        var term = search?.Trim();
        if (!string.IsNullOrEmpty(term))
            // Contains becomes SQL LIKE; the database collation makes it case-insensitive
            itemsQuery = itemsQuery.Where(i => i.Name.Contains(term));

        var items = await itemsQuery.OrderBy(i => i.Name).ToListAsync();

        // 4. Group the dishes under their category headings, skipping empty categories
        var sections = categories
            .Where(c => !categoryId.HasValue || c.MenuCategoryId == categoryId)
            .Select(c => new MenuCategorySectionViewModel
            {
                MenuCategoryId = c.MenuCategoryId,
                Name = c.Name,
                Description = c.Description,
                Items = items.Where(i => i.MenuCategoryId == c.MenuCategoryId).Select(ToCard).ToList()
            })
            .Where(s => s.Items.Count > 0)
            .ToList();

        return new MenuBrowseViewModel
        {
            Tabs = categories.Select(ToOption).ToList(),
            Sections = sections,
            SelectedCategoryId = categoryId,
            Search = term
        };
    }

    // =====================================================================
    // Admin: lists and lookups (PB06)
    // =====================================================================

    public async Task<List<AdminMenuItemRowViewModel>> GetAllItemsForAdminAsync()
    {
        var items = await _context.MenuItems
            .AsNoTracking()
            .Include(i => i.MenuCategory)
            .OrderBy(i => i.MenuCategory!.DisplayOrder).ThenBy(i => i.Name) // ! is fine: inside a query, EF handles nulls
            .ToListAsync();

        return items.Select(ToRow).ToList();
    }

    public async Task<List<AdminMenuCategoryRowViewModel>> GetAllCategoriesForAdminAsync()
    {
        return await _context.MenuCategories
            .AsNoTracking()
            .OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name)
            .Select(c => new AdminMenuCategoryRowViewModel
            {
                MenuCategoryId = c.MenuCategoryId,
                Name = c.Name,
                Description = c.Description,
                DisplayOrder = c.DisplayOrder,
                IsActive = c.IsActive,
                ItemCount = c.MenuItems.Count
            })
            .ToListAsync();
    }

    public async Task<List<MenuCategoryOptionViewModel>> GetCategoryOptionsAsync()
    {
        var categories = await _context.MenuCategories
            .AsNoTracking()
            .OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name)
            .ToListAsync();

        return categories.Select(ToOption).ToList();
    }

    public async Task<AdminMenuItemRowViewModel?> GetItemRowAsync(int menuItemId)
    {
        var item = await _context.MenuItems
            .AsNoTracking()
            .Include(i => i.MenuCategory)
            .FirstOrDefaultAsync(i => i.MenuItemId == menuItemId);

        return item == null ? null : ToRow(item);
    }

    public async Task<AdminMenuCategoryRowViewModel?> GetCategoryRowAsync(int menuCategoryId)
    {
        return await _context.MenuCategories
            .AsNoTracking()
            .Where(c => c.MenuCategoryId == menuCategoryId)
            .Select(c => new AdminMenuCategoryRowViewModel
            {
                MenuCategoryId = c.MenuCategoryId,
                Name = c.Name,
                Description = c.Description,
                DisplayOrder = c.DisplayOrder,
                IsActive = c.IsActive,
                ItemCount = c.MenuItems.Count
            })
            .FirstOrDefaultAsync();
    }

    public async Task<MenuItemInput?> GetItemInputAsync(int menuItemId)
    {
        var item = await _context.MenuItems.AsNoTracking()
            .FirstOrDefaultAsync(i => i.MenuItemId == menuItemId);
        if (item == null) return null;

        return new MenuItemInput
        {
            MenuItemId = item.MenuItemId,
            MenuCategoryId = item.MenuCategoryId,
            Name = item.Name,
            Description = item.Description,
            Price = item.Price,
            IsAvailable = item.IsAvailable
        };
    }

    public async Task<MenuCategoryInput?> GetCategoryInputAsync(int menuCategoryId)
    {
        var category = await _context.MenuCategories.AsNoTracking()
            .FirstOrDefaultAsync(c => c.MenuCategoryId == menuCategoryId);
        if (category == null) return null;

        return new MenuCategoryInput
        {
            MenuCategoryId = category.MenuCategoryId,
            Name = category.Name,
            Description = category.Description,
            DisplayOrder = category.DisplayOrder,
            IsActive = category.IsActive
        };
    }

    // =====================================================================
    // Admin: items (PB06)
    // =====================================================================

    public async Task<(bool Success, string Message)> CreateItemAsync(MenuItemInput input)
    {
        // 1. Business checks the input model cannot do on its own
        var name = input.Name.Trim();
        var error = await ValidateItemAsync(input.MenuItemId, input.MenuCategoryId, name, input.ImageFile);
        if (error != null) return (false, error);

        // 2. Save the image first, so the item is only created with a working image path
        var imageUrl = input.ImageFile != null
            ? await _imageStorage.SaveMenuImageAsync(input.ImageFile)
            : string.Empty;

        // 3. Copy the input onto a new entity and save
        _context.MenuItems.Add(new MenuItem
        {
            MenuCategoryId = input.MenuCategoryId,
            Name = name,
            Description = input.Description?.Trim() ?? string.Empty,
            Price = input.Price,
            ImageUrl = imageUrl,
            IsAvailable = input.IsAvailable
        });
        await _context.SaveChangesAsync();

        return (true, $"{name} added to the menu successfully!");
    }

    public async Task<(bool Success, string Message)> UpdateItemAsync(MenuItemInput input)
    {
        // 1. Re-fetch the row and copy fields onto it (lab pattern), rather than attaching the posted data
        var item = await _context.MenuItems.FirstOrDefaultAsync(i => i.MenuItemId == input.MenuItemId);
        if (item == null) return (false, "That menu item no longer exists.");

        // 2. Business checks
        var name = input.Name.Trim();
        var error = await ValidateItemAsync(item.MenuItemId, input.MenuCategoryId, name, input.ImageFile);
        if (error != null) return (false, error);

        // 3. Save a new image if one was uploaded; remember the old one to delete after saving
        string? oldImageUrl = null;
        if (input.ImageFile != null)
        {
            oldImageUrl = item.ImageUrl;
            item.ImageUrl = await _imageStorage.SaveMenuImageAsync(input.ImageFile);
        }

        // 4. Copy the edited fields and save
        item.MenuCategoryId = input.MenuCategoryId;
        item.Name = name;
        item.Description = input.Description?.Trim() ?? string.Empty;
        item.Price = input.Price;
        item.IsAvailable = input.IsAvailable;
        await _context.SaveChangesAsync();

        // 5. Only now is it safe to delete the old file
        _imageStorage.DeleteMenuImage(oldImageUrl);

        return (true, $"{name} updated successfully!");
    }

    public async Task<string?> GetItemDeleteBlockReasonAsync(int menuItemId)
    {
        // Rule: an item that appears on any order is kept, so past orders and receipts stay intact
        var hasOrders = await _context.OrderItems.AnyAsync(oi => oi.MenuItemId == menuItemId);
        return hasOrders
            ? "This item has order history, so it cannot be deleted. Mark it as sold out instead."
            : null;
    }

    public async Task<(bool Success, string Message)> DeleteItemAsync(int menuItemId)
    {
        var item = await _context.MenuItems.FirstOrDefaultAsync(i => i.MenuItemId == menuItemId);
        if (item == null) return (false, "That menu item no longer exists.");

        // Check the rule again on POST: the page could be stale, or the request crafted
        var blockReason = await GetItemDeleteBlockReasonAsync(menuItemId);
        if (blockReason != null) return (false, blockReason);

        _context.MenuItems.Remove(item);
        await _context.SaveChangesAsync();
        _imageStorage.DeleteMenuImage(item.ImageUrl);

        return (true, $"{item.Name} deleted successfully!");
    }

    public async Task<(bool Success, string Message)> ToggleItemAvailabilityAsync(int menuItemId)
    {
        var item = await _context.MenuItems.FirstOrDefaultAsync(i => i.MenuItemId == menuItemId);
        if (item == null) return (false, "That menu item no longer exists.");

        item.IsAvailable = !item.IsAvailable;
        await _context.SaveChangesAsync();

        return (true, item.IsAvailable
            ? $"{item.Name} is available again."
            : $"{item.Name} is now marked as sold out.");
    }

    // =====================================================================
    // Admin: categories (PB06)
    // =====================================================================

    public async Task<(bool Success, string Message)> CreateCategoryAsync(MenuCategoryInput input)
    {
        var name = input.Name.Trim();
        if (await CategoryNameTakenAsync(name, excludeId: 0))
            return (false, $"A category called \"{name}\" already exists.");

        _context.MenuCategories.Add(new MenuCategory
        {
            Name = name,
            Description = input.Description?.Trim() ?? string.Empty,
            DisplayOrder = input.DisplayOrder,
            IsActive = input.IsActive
        });
        await _context.SaveChangesAsync();

        return (true, $"Category {name} created successfully!");
    }

    public async Task<(bool Success, string Message)> UpdateCategoryAsync(MenuCategoryInput input)
    {
        var category = await _context.MenuCategories
            .FirstOrDefaultAsync(c => c.MenuCategoryId == input.MenuCategoryId);
        if (category == null) return (false, "That category no longer exists.");

        var name = input.Name.Trim();
        if (await CategoryNameTakenAsync(name, excludeId: category.MenuCategoryId))
            return (false, $"A category called \"{name}\" already exists.");

        category.Name = name;
        category.Description = input.Description?.Trim() ?? string.Empty;
        category.DisplayOrder = input.DisplayOrder;
        category.IsActive = input.IsActive;
        await _context.SaveChangesAsync();

        return (true, $"Category {name} updated successfully!");
    }

    public async Task<string?> GetCategoryDeleteBlockReasonAsync(int menuCategoryId)
    {
        // Rule: a category must be empty before it can be deleted, so no dish is left without one
        var hasItems = await _context.MenuItems.AnyAsync(i => i.MenuCategoryId == menuCategoryId);
        return hasItems
            ? "This category still has dishes. Move or delete them first, or untick \"Show this category on the menu\" to hide it."
            : null;
    }

    public async Task<(bool Success, string Message)> DeleteCategoryAsync(int menuCategoryId)
    {
        var category = await _context.MenuCategories
            .FirstOrDefaultAsync(c => c.MenuCategoryId == menuCategoryId);
        if (category == null) return (false, "That category no longer exists.");

        var blockReason = await GetCategoryDeleteBlockReasonAsync(menuCategoryId);
        if (blockReason != null) return (false, blockReason);

        _context.MenuCategories.Remove(category);
        await _context.SaveChangesAsync();

        return (true, $"Category {category.Name} deleted successfully!");
    }

    // =====================================================================
    // Private helpers
    // =====================================================================

    // Shared create/edit checks for an item. Returns an error message, or null if everything is fine.
    private async Task<string?> ValidateItemAsync(int menuItemId, int menuCategoryId, string name, IFormFile? imageFile)
    {
        if (!await _context.MenuCategories.AnyAsync(c => c.MenuCategoryId == menuCategoryId))
            return "Please choose a valid category.";

        // Same dish name twice in one category would confuse customers; the SQL collation makes this case-insensitive
        var nameTaken = await _context.MenuItems.AnyAsync(i =>
            i.MenuCategoryId == menuCategoryId && i.Name == name && i.MenuItemId != menuItemId);
        if (nameTaken)
            return $"\"{name}\" already exists in this category.";

        return imageFile == null ? null : _imageStorage.ValidateImage(imageFile);
    }

    private async Task<bool> CategoryNameTakenAsync(string name, int excludeId)
    {
        return await _context.MenuCategories.AnyAsync(c => c.Name == name && c.MenuCategoryId != excludeId);
    }

    private MenuItemCardViewModel ToCard(MenuItem item) => new()
    {
        MenuItemId = item.MenuItemId,
        Name = item.Name,
        Description = item.Description,
        Price = item.Price,
        ImageUrl = ImageOrPlaceholder(item.ImageUrl),
        IsAvailable = item.IsAvailable
    };

    private AdminMenuItemRowViewModel ToRow(MenuItem item) => new()
    {
        MenuItemId = item.MenuItemId,
        Name = item.Name,
        CategoryName = item.MenuCategory?.Name ?? string.Empty,
        Price = item.Price,
        ImageUrl = ImageOrPlaceholder(item.ImageUrl),
        IsAvailable = item.IsAvailable
    };

    private static MenuCategoryOptionViewModel ToOption(MenuCategory category) => new()
    {
        MenuCategoryId = category.MenuCategoryId,
        Name = category.Name,
        IsActive = category.IsActive
    };

    private string ImageOrPlaceholder(string imageUrl) =>
        string.IsNullOrWhiteSpace(imageUrl) ? _imageStorage.PlaceholderImageUrl : imageUrl;
}
