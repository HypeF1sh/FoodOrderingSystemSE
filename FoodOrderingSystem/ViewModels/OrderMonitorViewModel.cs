namespace FoodOrderingSystem.ViewModels;   // <-- match your project namespace

// PB21: result of the admin order monitor (read-only)
public class OrderMonitorViewModel
{
    // Orders that match the date range AND the status filter, newest first
    public List<StaffOrderViewModel> Orders { get; set; } = new();

    // Count of ALL orders in the date range (ignores the status filter)
    public int TotalInRange { get; set; }

    // Status name -> count (all statuses, in enum order)
    public Dictionary<string, int> StatusCounts { get; set; } = new();

    // Value of the orders in the range, not counting Cancelled ones
    public decimal TotalValue { get; set; }
}
