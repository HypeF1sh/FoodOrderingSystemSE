// PB22 (Jordan) - view model: dashboard view model
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodOrderingSystem.ViewModels;

// Everything the admin dashboard shows for one date range (PB22). Built by ReportService, read-only.
public class DashboardViewModel
{
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
    public ReportGrouping Grouping { get; set; }

    // ---- Headline numbers ----
    [Column(TypeName = "decimal(10,2)")]
    public decimal Revenue { get; set; }              // Paid payments in the range
    public int PaidOrderCount { get; set; }           // Orders with a Paid payment in the range

    [Column(TypeName = "decimal(10,2)")]
    public decimal AverageOrderValue { get; set; }    // Revenue ÷ paid orders
    public int OrderCount { get; set; }               // Orders placed in the range, not Cancelled
    public int BookingCount { get; set; }             // Reservations in the range, not Cancelled
    public int CoverCount { get; set; }               // Guests across those bookings

    // ---- Charts ----
    public List<ReportPointViewModel> SalesByPeriod { get; set; } = new();
    public List<ReportPointViewModel> BookingsByPeriod { get; set; } = new();
    public List<PopularItemViewModel> PopularItems { get; set; } = new();
}
