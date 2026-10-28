using FoodOrderingSystem.Models.Enums;

namespace FoodOrderingSystem.ViewModels;

// One order's bill for staff billing (Wallace's PB17 page reads it through IPaymentService)
public class BillViewModel
{
    public int OrderId { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public OrderStatus OrderStatus { get; set; }
    public decimal TotalAmount { get; set; }
    public List<OrderLineViewModel> Lines { get; set; } = new();
    public List<PaymentAttemptViewModel> Attempts { get; set; } = new();
    public OrderPaymentSummary Payment { get; set; } = new();

    // Same rule as the customer Pay page: not cancelled, not already paid
    public bool CanRecordPayment { get; set; }
    public string? CannotPayReason { get; set; }
}
