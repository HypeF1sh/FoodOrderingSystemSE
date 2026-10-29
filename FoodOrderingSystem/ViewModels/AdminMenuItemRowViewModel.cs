namespace FoodOrderingSystem.ViewModels;

// One row of the Items table on /Admin/Menu, also used by the Delete Item page
public class AdminMenuItemRowViewModel
{
    public int MenuItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
}
