namespace FoodOrderingSystem.ViewModels;

// One dish on a placed order, at the price it was ordered at
public class OrderLineViewModel
{
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    public decimal LineTotal => UnitPrice * Quantity;
}
