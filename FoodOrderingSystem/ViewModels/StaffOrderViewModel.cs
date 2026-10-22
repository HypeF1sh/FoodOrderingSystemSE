using FoodOrderingSystem.Models.Enums;

namespace FoodOrderingSystem.ViewModels;

// One order card on the staff board (PB15)
public class StaffOrderViewModel
{
    public int OrderId { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int? ReservationId { get; set; }
    public OrderStatus Status { get; set; }
    public decimal TotalAmount { get; set; }
    public string Notes { get; set; } = string.Empty;
    public List<OrderLineViewModel> Lines { get; set; } = new();

    // Worked out by the service from the transition rules, so the page never decides what is allowed
    public OrderStatus? NextStatus { get; set; }
    public bool CanCancel { get; set; }
}
