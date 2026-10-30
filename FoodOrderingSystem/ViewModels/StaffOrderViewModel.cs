using FoodOrderingSystem.Models.Enums;

namespace FoodOrderingSystem.ViewModels;

// One order card on the staff board (PB15)
public class StaffOrderViewModel
{
    public int OrderId { get; set; }
    public DateTime CreatedAt { get; set; }

    // CustomerId is for grouping by customer in admin reports (PB23; names are not unique); CustomerName is for display
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public int? ReservationId { get; set; }
    public OrderStatus Status { get; set; }
    public decimal TotalAmount { get; set; }
    public string Notes { get; set; } = string.Empty;
    public List<OrderLineViewModel> Lines { get; set; } = new();

    // PB16: shown as a badge so staff can see who still owes. Paid if any payment attempt succeeded.
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid;
    public bool IsPaid => PaymentStatus == PaymentStatus.Paid;

    // Worked out by the service from the transition rules, so the page never decides what is allowed
    public OrderStatus? NextStatus { get; set; }
    public bool CanCancel { get; set; }
}
