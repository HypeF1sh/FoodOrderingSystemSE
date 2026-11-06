// PB16 (Jordan) - view model: payment attempt view model
using FoodOrderingSystem.Models.Enums;

namespace FoodOrderingSystem.ViewModels;

// One row in the "Payment attempts" list: every try is kept, so a declined card shows as Failed
public class PaymentAttemptViewModel
{
    public int PaymentId { get; set; }
    public DateTime AttemptedAt { get; set; }
    public PaymentMethod Method { get; set; }
    public PaymentStatus Status { get; set; }
    public decimal Amount { get; set; }
    public string TransactionRef { get; set; } = string.Empty;
}
