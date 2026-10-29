namespace FoodOrderingSystem.ViewModels;

// The whole cart as shown on the cart and checkout pages
public class CartViewModel
{
    public List<CartLineViewModel> Lines { get; set; } = new();

    public bool IsEmpty => Lines.Count == 0;
    public int ItemCount => Lines.Sum(l => l.Quantity);

    // Only available dishes count towards the total; sold-out lines must be removed before checkout
    public decimal Total => Lines.Where(l => l.IsAvailable).Sum(l => l.LineTotal);
    public bool HasUnavailableItems => Lines.Any(l => !l.IsAvailable);
}
