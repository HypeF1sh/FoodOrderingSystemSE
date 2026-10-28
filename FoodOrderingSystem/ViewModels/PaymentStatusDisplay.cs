using FoodOrderingSystem.Models.Enums;

namespace FoodOrderingSystem.ViewModels;

// The words and badge colours for payments, kept in one place like OrderStatusDisplay
public static class PaymentStatusDisplay
{
    public static string MethodLabel(PaymentMethod method) => method switch
    {
        PaymentMethod.EWallet => "E-wallet",
        _ => method.ToString()
    };

    public static string BadgeClass(PaymentStatus status) => status switch
    {
        PaymentStatus.Paid => "bg-success",
        PaymentStatus.Failed => "bg-danger",
        PaymentStatus.Refunded => "bg-secondary",
        _ => "bg-warning text-dark"   // Unpaid
    };
}
