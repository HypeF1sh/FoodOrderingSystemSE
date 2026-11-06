// PB06/PB08 (Jordan) - menu (interface)
using FoodOrderingSystem.ViewModels;

namespace FoodOrderingSystem.Services;

public interface IMenuService
{
    // ---- Customer browsing (PB08) ----

    /// <summary>Active categories and their dishes, optionally filtered by category and a name search.
    /// Unavailable dishes are included so the page can show them as sold out.</summary>
    Task<MenuBrowseViewModel> GetMenuForBrowseAsync(int? categoryId, string? search);

    // ---- Admin: lists and lookups (PB06) ----

    /// <summary>Every menu item, including unavailable ones, for the admin Items table.</summary>
    Task<List<AdminMenuItemRowViewModel>> GetAllItemsForAdminAsync();

    /// <summary>Every category, including inactive ones, with its item count.</summary>
    Task<List<AdminMenuCategoryRowViewModel>> GetAllCategoriesForAdminAsync();

    /// <summary>All categories for the item form dropdown, in display order.</summary>
    Task<List<MenuCategoryOptionViewModel>> GetCategoryOptionsAsync();

    /// <summary>One item as a display row, or null if it does not exist.</summary>
    Task<AdminMenuItemRowViewModel?> GetItemRowAsync(int menuItemId);

    /// <summary>One category as a display row, or null if it does not exist.</summary>
    Task<AdminMenuCategoryRowViewModel?> GetCategoryRowAsync(int menuCategoryId);

    /// <summary>An item's current values for the edit form, or null if it does not exist.</summary>
    Task<MenuItemInput?> GetItemInputAsync(int menuItemId);

    /// <summary>A category's current values for the edit form, or null if it does not exist.</summary>
    Task<MenuCategoryInput?> GetCategoryInputAsync(int menuCategoryId);

    // ---- Admin: items (PB06) ----

    /// <summary>Creates an item, saving its image if one was uploaded. Returns (success, message).</summary>
    Task<(bool Success, string Message)> CreateItemAsync(MenuItemInput input);

    /// <summary>Updates an item; a new image replaces (and deletes) the old one. Returns (success, message).</summary>
    Task<(bool Success, string Message)> UpdateItemAsync(MenuItemInput input);

    /// <summary>Why the item cannot be deleted, or null if it can.</summary>
    Task<string?> GetItemDeleteBlockReasonAsync(int menuItemId);

    /// <summary>Deletes an item that has never been ordered, and its image. Returns (success, message).</summary>
    Task<(bool Success, string Message)> DeleteItemAsync(int menuItemId);

    /// <summary>Flips an item between available and sold out. Returns (success, message).</summary>
    Task<(bool Success, string Message)> ToggleItemAvailabilityAsync(int menuItemId);

    // ---- Admin: categories (PB06) ----

    /// <summary>Creates a category with a unique name. Returns (success, message).</summary>
    Task<(bool Success, string Message)> CreateCategoryAsync(MenuCategoryInput input);

    /// <summary>Updates a category, keeping its name unique. Returns (success, message).</summary>
    Task<(bool Success, string Message)> UpdateCategoryAsync(MenuCategoryInput input);

    /// <summary>Why the category cannot be deleted, or null if it can.</summary>
    Task<string?> GetCategoryDeleteBlockReasonAsync(int menuCategoryId);

    /// <summary>Deletes a category that has no items. Returns (success, message).</summary>
    Task<(bool Success, string Message)> DeleteCategoryAsync(int menuCategoryId);
}
