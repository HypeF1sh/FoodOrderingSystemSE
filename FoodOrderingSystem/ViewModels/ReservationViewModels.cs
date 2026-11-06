// PB09-PB13 (Wallace) - view model: reservation view models
namespace FoodOrderingSystem.ViewModels;

public class DiningAreaOptionViewModel
{
    public int DiningAreaId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class AvailableTableViewModel
{
    public int RestaurantTableId { get; set; }
    public string TableNumber { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public int DiningAreaId { get; set; }
    public string DiningAreaName { get; set; } = string.Empty;
}

public class ReservationDetailViewModel
{
    public int ReservationId { get; set; }
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public int RestaurantTableId { get; set; }
    public string TableNumber { get; set; } = string.Empty;
    public int TableCapacity { get; set; }
    public int DiningAreaId { get; set; }
    public string DiningAreaName { get; set; } = string.Empty;
    public DateOnly ReservationDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int PartySize { get; set; }
    public string Status { get; set; } = string.Empty;
    public string SpecialRequests { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public bool CanModifyOrCancel { get; set; }
    public string? LockReason { get; set; }
}

// PB13: Holds a customer's dining preferences, special requests, and full reservation history for staff review.
public class CustomerReservationProfileViewModel
{
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;

    public int TotalBookings { get; set; }
    public int CompletedCount { get; set; }
    public int CancelledCount { get; set; }
    public int NoShowCount { get; set; }

    public string PreferredDiningArea { get; set; } = "No preference yet";
    public int TypicalPartySize { get; set; }
    public List<string> NotableRequests { get; set; } = new();

    public List<ReservationDetailViewModel> Reservations { get; set; } = new();
}