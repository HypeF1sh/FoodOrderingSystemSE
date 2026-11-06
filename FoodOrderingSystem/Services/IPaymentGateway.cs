// PB16 (Jordan) - payment gateway (interface)
using FoodOrderingSystem.ViewModels;

namespace FoodOrderingSystem.Services;

// The outside payment provider (PB16). PaymentService only knows this interface, so the simulated
// gateway could be swapped for a real one (e.g. Stripe) in Program.cs without touching pages or PaymentService.
public interface IPaymentGateway
{
    /// <summary>Asks the provider to take the money. Never touches the database.
    /// Returns whether it was approved, the provider's reference, and why it was declined (if it was).</summary>
    Task<(bool Approved, string Reference, string? DeclineReason)> ChargeAsync(decimal amount, PaymentInput details);
}
