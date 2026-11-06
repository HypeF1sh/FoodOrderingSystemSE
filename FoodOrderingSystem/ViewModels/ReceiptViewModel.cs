// PB16 (Jordan) - view model: receipt view model
using FoodOrderingSystem.Models.Enums;

namespace FoodOrderingSystem.ViewModels;

// One receipt (D4, folded into PB16). Only Paid (or later Refunded) payments have one.
public class ReceiptViewModel
{
    public int PaymentId { get; set; }
    public string RestaurantName { get; set; } = string.Empty;
    public int OrderId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public DateTime PaidAt { get; set; }
    public List<OrderLineViewModel> Lines { get; set; } = new();
    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; }
    public PaymentStatus Status { get; set; }
    public string TransactionRef { get; set; } = string.Empty;

    // Cash only: the staff member who took the money
    public string? RecordedByName { get; set; }

    // Cash only, and only right after recording: not stored in the database, passed in by the page
    public decimal? AmountTendered { get; set; }
    public decimal? Change { get; set; }

    // Receipt R-000123 is simply the payment id, padded
    public string ReceiptNumber => $"R-{PaymentId:D6}";

    // The order's notes. When a promo code was used, PlaceOrderAsync wrote it at the start ("Promo PROMO3 (name): 10% off | ...")
    public string Notes { get; set; } = string.Empty;
    public string? PromotionLine =>
        Notes.StartsWith("Promo ", StringComparison.Ordinal) ? Notes.Split(" | ")[0] : null;
}
