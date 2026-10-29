namespace FoodOrderingSystem.ViewModels;   // <-- match your project namespace

// PB21: result of the admin reservation monitor (read-only)
public class ReservationMonitorViewModel
{
    // Reservations that match the date range AND the status filter, oldest first
    public List<ReservationDetailViewModel> Reservations { get; set; } = new();

    // Count of ALL reservations in the date range (ignores the status filter)
    public int TotalInRange { get; set; }

    // Status name -> count (all statuses, in enum order), for the summary badges
    public Dictionary<string, int> StatusCounts { get; set; } = new();
}
