using System.Text.Json;
using FoodOrderingSystem.Data;
using FoodOrderingSystem.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace FoodOrderingSystem.Services;

public class CartService : ICartService
{
    // Session key and limits, kept here so the rules live in one place
    private const string SessionKey = "Cart";
    public const int MaxQuantityPerLine = 20;
    public const int MaxLines = 30;

    private readonly ApplicationDbContext _context;
    private readonly IImageStorageService _imageStorage;

    public CartService(ApplicationDbContext context, IImageStorageService imageStorage)
    {
        _context = context;
        _imageStorage = imageStorage;
    }

    public async Task<CartViewModel> GetCartAsync(ISession session)
    {
        var items = GetItems(session);
        if (items.Count == 0) return new CartViewModel();

        // 1. Look up every dish in the cart in one query, for its current price and availability
        var ids = items.Select(i => i.MenuItemId).ToList();
        var dishes = await _context.MenuItems
            .AsNoTracking()
            .Include(m => m.MenuCategory)
            .Where(m => ids.Contains(m.MenuItemId))
            .ToDictionaryAsync(m => m.MenuItemId);

        // 2. Build the rows in the order they were added; skip dishes that no longer exist
        var lines = new List<CartLineViewModel>();
        foreach (var item in items)
        {
            if (!dishes.TryGetValue(item.MenuItemId, out var dish)) continue;

            lines.Add(new CartLineViewModel
            {
                MenuItemId = dish.MenuItemId,
                Name = dish.Name,
                ImageUrl = string.IsNullOrEmpty(dish.ImageUrl) ? _imageStorage.PlaceholderImageUrl : dish.ImageUrl,
                UnitPrice = dish.Price,
                Quantity = item.Quantity,
                // A dish in a hidden category counts as unavailable too
                IsAvailable = dish.IsAvailable && dish.MenuCategory != null && dish.MenuCategory.IsActive
            });
        }

        // 3. If a deleted dish was dropped, save the cleaned-up cart back
        if (lines.Count != items.Count)
            SaveItems(session, items.Where(i => dishes.ContainsKey(i.MenuItemId)).ToList());

        return new CartViewModel { Lines = lines };
    }

    public List<CartSessionItem> GetItems(ISession session)
    {
        var json = session.GetString(SessionKey);
        if (string.IsNullOrEmpty(json)) return new List<CartSessionItem>();

        // A damaged value is treated as an empty cart rather than crashing the page
        try
        {
            return JsonSerializer.Deserialize<List<CartSessionItem>>(json) ?? new List<CartSessionItem>();
        }
        catch (JsonException)
        {
            return new List<CartSessionItem>();
        }
    }

    public int GetItemCount(ISession session) => GetItems(session).Sum(i => i.Quantity);

    public async Task<(bool Success, string Message)> AddItemAsync(ISession session, int menuItemId, int quantity)
    {
        // 1. The dish must exist, be available, and be in a category customers can see
        var dish = await _context.MenuItems
            .AsNoTracking()
            .Include(m => m.MenuCategory)
            .FirstOrDefaultAsync(m => m.MenuItemId == menuItemId);

        if (dish == null || dish.MenuCategory == null || !dish.MenuCategory.IsActive)
            return (false, "That dish could not be found.");
        if (!dish.IsAvailable)
            return (false, $"Sorry, {dish.Name} is sold out.");

        // 2. Quantity from the form is clamped, so a crafted request cannot add 999
        quantity = Math.Clamp(quantity, 1, MaxQuantityPerLine);

        // 3. Merge with an existing line, or add a new one
        var items = GetItems(session);
        var line = items.FirstOrDefault(i => i.MenuItemId == menuItemId);

        if (line == null)
        {
            if (items.Count >= MaxLines)
                return (false, $"Your cart can hold up to {MaxLines} different dishes.");
            items.Add(new CartSessionItem { MenuItemId = menuItemId, Quantity = quantity });
        }
        else
        {
            if (line.Quantity >= MaxQuantityPerLine)
                return (false, $"You can order up to {MaxQuantityPerLine} of {dish.Name}.");
            line.Quantity = Math.Min(line.Quantity + quantity, MaxQuantityPerLine);
        }

        SaveItems(session, items);
        return (true, $"Added {dish.Name} to your cart.");
    }

    public (bool Success, string Message) ChangeQuantity(ISession session, int menuItemId, int delta)
    {
        var items = GetItems(session);
        var line = items.FirstOrDefault(i => i.MenuItemId == menuItemId);
        if (line == null) return (false, "That dish is not in your cart.");

        var newQuantity = line.Quantity + delta;
        if (newQuantity > MaxQuantityPerLine)
            return (false, $"You can order up to {MaxQuantityPerLine} of each dish.");

        // Going down to 0 removes the line
        if (newQuantity <= 0)
            items.Remove(line);
        else
            line.Quantity = newQuantity;

        SaveItems(session, items);
        return (true, string.Empty);
    }

    public void RemoveItem(ISession session, int menuItemId)
    {
        var items = GetItems(session);
        items.RemoveAll(i => i.MenuItemId == menuItemId);
        SaveItems(session, items);
    }

    public void Clear(ISession session) => session.Remove(SessionKey);

    private static void SaveItems(ISession session, List<CartSessionItem> items)
    {
        if (items.Count == 0)
            session.Remove(SessionKey);
        else
            session.SetString(SessionKey, JsonSerializer.Serialize(items));
    }
}
