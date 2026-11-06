// PB23 (Carynn) - view model: customer activity row view model
namespace FoodOrderingSystem.ViewModels;   // <-- match your project namespace

// PB23: one customer's activity in a date range
public class CustomerActivityRowViewModel
{
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerEmail { get; set; }

    public int ReservationCount { get; set; }   // reservations in the range, not counting Cancelled
    public int OrderCount { get; set; }         // orders in the range, not counting Cancelled
    public decimal TotalSpent { get; set; }     // sum of Paid payments in the range
    public DateOnly? LastVisit { get; set; }    // latest Completed reservation or Paid payment in the range

    public int ActivityCount => ReservationCount + OrderCount;
}
