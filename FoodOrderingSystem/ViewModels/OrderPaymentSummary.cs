// PB14/PB18 (Jordan) - view model: order payment summary
using FoodOrderingSystem.Models.Enums;

namespace FoodOrderingSystem.ViewModels;

// An order's overall payment state, worked out from all its payment attempts (PB16)
public class OrderPaymentSummary
{
    public PaymentStatus Status { get; set; } = PaymentStatus.Unpaid;

    // The successful payment, for the "View receipt" link. Null unless Status is Paid.
    public int? PaidPaymentId { get; set; }

    public bool IsPaid => Status == PaymentStatus.Paid;
}
