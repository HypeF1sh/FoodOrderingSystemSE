using FoodOrderingSystem.Models.Enums;

namespace FoodOrderingSystem.ViewModels;

// One row in the staff "still to pay" list (PB17, Wallace)
public class UnpaidOrderRowViewModel
{
    public int OrderId { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int? ReservationId { get; set; }
    public OrderStatus OrderStatus { get; set; }
    public decimal TotalAmount { get; set; }

    // True when the customer tried to pay online and it was declined
    public bool HasFailedAttempt { get; set; }
}
