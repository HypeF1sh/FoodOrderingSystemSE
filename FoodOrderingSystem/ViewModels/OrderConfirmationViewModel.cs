using FoodOrderingSystem.Models.Enums;

namespace FoodOrderingSystem.ViewModels;

// A placed order as shown on the confirmation page
public class OrderConfirmationViewModel
{
    public int OrderId { get; set; }
    public DateTime CreatedAt { get; set; }
    public OrderStatus Status { get; set; }
    public decimal TotalAmount { get; set; }
    public string Notes { get; set; } = string.Empty;
    public List<OrderLineViewModel> Lines { get; set; } = new();
}
