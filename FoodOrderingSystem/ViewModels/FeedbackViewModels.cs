namespace FoodOrderingSystem.ViewModels;

public class EligibleVisitOptionViewModel
{
    public int? ReservationId { get; set; }
    public int? OrderId { get; set; }
    public string DisplayLabel { get; set; } = string.Empty;
}

public class FeedbackItemViewModel
{
    public int FeedbackId { get; set; }
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public int? ReservationId { get; set; }
    public int? OrderId { get; set; }
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}