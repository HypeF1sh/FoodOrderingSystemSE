namespace FoodOrderingSystem.ViewModels;

// One table tile on the staff Floor, plus what its drawer shows. Filled by FloorService.
public class FloorTableViewModel
{
    public int RestaurantTableId { get; set; }
    public string TableNumber { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public int DiningAreaId { get; set; }

    public TableState State { get; set; } = TableState.Free;
    public string Subtitle { get; set; } = string.Empty;
    public bool IsPendingBooking { get; set; }   // amber dot on a Booked tile

    // The booking that decides the state; all null when the table is free
    public int? ReservationId { get; set; }
    public string? CustomerName { get; set; }
    public int? PartySize { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }

    public List<StaffOrderViewModel> Orders { get; set; } = new();   // today's orders on this table, not yet Completed
    public List<StaffOrderViewModel> HeldOrders { get; set; } = new();   // pre-orders waiting for Send to kitchen
    public decimal UnpaidTotal { get; set; }
    public List<string> LaterToday { get; set; } = new();            // other bookings for this table later today

    // The one next-step button in the drawer
    public TableAction Action { get; set; } = TableAction.None;
    public int? ActionOrderId { get; set; }
}
