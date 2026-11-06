// PB22 (Jordan) - view model: report point view model
namespace FoodOrderingSystem.ViewModels;

// One bar on a dashboard chart (PB22), e.g. "28 Oct" → RM 412.50, or "Confirmed" → 6 bookings
public class ReportPointViewModel
{
    public string Label { get; set; } = string.Empty;
    public decimal Value { get; set; }
}
