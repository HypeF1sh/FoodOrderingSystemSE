using FoodOrderingSystem.Models.Enums;

namespace FoodOrderingSystem.ViewModels;

// One order in the customer's "My orders" list (PB18)
public class CustomerOrderRowViewModel
{
    public int OrderId { get; set; }
    public DateTime CreatedAt { get; set; }
    public OrderStatus Status { get; set; }
    public decimal TotalAmount { get; set; }
    public List<OrderLineViewModel> Lines { get; set; } = new();

    // Set by OrderService: still with the kitchen (Pending to Served), so its status is polled for changes
    public bool IsActive { get; set; }

    // Filled in by the page from IPaymentService, because payments are a separate service
    public OrderPaymentSummary Payment { get; set; } = new();
}
