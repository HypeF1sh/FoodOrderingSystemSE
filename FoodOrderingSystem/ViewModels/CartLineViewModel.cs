// PB14 (Jordan) - view model: cart line view model
namespace FoodOrderingSystem.ViewModels;

// One row on the cart page, with the dish's current name, price and availability
public class CartLineViewModel
{
    public int MenuItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public bool IsAvailable { get; set; }

    public decimal LineTotal => UnitPrice * Quantity;
}
