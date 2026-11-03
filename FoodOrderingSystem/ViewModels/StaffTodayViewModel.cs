namespace FoodOrderingSystem.ViewModels;

// The staff Today page: counts (never money), today's bookings in time order, and the full Needs-you list
public class StaffTodayViewModel
{
    public DateTime Now { get; set; }

    public int BookingsToday { get; set; }
    public int ArrivingSoon { get; set; }
    public int ActiveOrders { get; set; }
    public int BillsDue { get; set; }

    public List<ReservationDetailViewModel> Timeline { get; set; } = new();
    public List<NeedsYouItemViewModel> NeedsYou { get; set; } = new();
}
