using FoodOrderingSystem.Models.Enums;

namespace FoodOrderingSystem.ViewModels;

// Everything the Pay page shows apart from the form itself (PB16)
public class PaymentPageViewModel
{
    public int OrderId { get; set; }
    public DateTime CreatedAt { get; set; }
    public OrderStatus OrderStatus { get; set; }
    public decimal TotalAmount { get; set; }
    public List<OrderLineViewModel> Lines { get; set; } = new();
    public List<PaymentAttemptViewModel> Attempts { get; set; } = new();
    public OrderPaymentSummary Payment { get; set; } = new();

    // Decided by the service: when false the page shows CannotPayReason instead of the form
    public bool CanPay { get; set; }
    public string? CannotPayReason { get; set; }

    // The order's notes. When a promo code was used, PlaceOrderAsync wrote it at the start ("Promo PROMO3 (name): 10% off | ...")
    public string Notes { get; set; } = string.Empty;
    public string? PromotionLine =>
        Notes.StartsWith("Promo ", StringComparison.Ordinal) ? Notes.Split(" | ")[0] : null;
}
