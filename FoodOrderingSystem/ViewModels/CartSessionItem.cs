namespace FoodOrderingSystem.ViewModels;

// One cart line as stored in the session (as JSON). Only the id and quantity are kept:
// names and prices are always read fresh from the database, so they cannot be tampered with.
public class CartSessionItem
{
    public int MenuItemId { get; set; }
    public int Quantity { get; set; }
}
