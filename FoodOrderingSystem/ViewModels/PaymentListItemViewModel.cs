// PB16 (Jordan) - view model: payment list item view model
using FoodOrderingSystem.Models.Enums;

namespace FoodOrderingSystem.ViewModels;

// One row in the admin payment list (PB21 / PB23, Carynn). Read-only: one row per payment attempt.
public class PaymentListItemViewModel
{
    public int PaymentId { get; set; }
    public int OrderId { get; set; }

    // CustomerId is for grouping by customer (names are not unique); CustomerName is for display
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;

    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; }
    public PaymentStatus Status { get; set; }
    public string TransactionRef { get; set; } = string.Empty;

    // Attempt time: for a Failed row this is when it was tried, not when it was paid
    public DateTime PaidAt { get; set; }
}
