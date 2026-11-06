// PB14 (Jordan) - view model: order target view model
namespace FoodOrderingSystem.ViewModels;

// Where an order placed now would go, shown on the checkout page (PB14)
public class OrderTargetViewModel
{
    // False when the customer is not at a table and has no confirmed booking: the Place order button is disabled
    public bool CanOrder { get; set; }
    public string Message { get; set; } = string.Empty;
}
