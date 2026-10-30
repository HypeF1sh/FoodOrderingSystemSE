namespace FoodOrderingSystem.ViewModels;   // <-- match your project namespace

// PB21: result of the admin payment monitor (read-only)
public class PaymentMonitorViewModel
{
    // Payments that match the date range AND the status filter, newest first
    public List<PaymentListItemViewModel> Payments { get; set; } = new();

    // Count of ALL payment attempts in the date range (ignores the status filter)
    public int TotalInRange { get; set; }

    // Status name -> count (all statuses, in enum order)
    public Dictionary<string, int> StatusCounts { get; set; } = new();

    public decimal PaidTotal { get; set; }       // sum of Paid payments in the range
    public decimal RefundedTotal { get; set; }   // sum of Refunded payments in the range
}
