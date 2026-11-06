// PB15/PB17 (Jordan) - view model: needs you item view model
namespace FoodOrderingSystem.ViewModels;

// One chip on the Floor's Needs-you bar, or one row in Today's Needs-you list
public class NeedsYouItemViewModel
{
    public int Priority { get; set; }            // 1 = most urgent, as in the spec's table
    public string Text { get; set; } = string.Empty;
    public string Tone { get; set; } = "plum";   // plum, jade or amber -> CSS class "is-<tone>"
    public bool HasAllergyNote { get; set; }
    public DateTime SortTime { get; set; }       // oldest first within the same priority

    // Where tapping it goes: the table on the Floor, or The Pass when the order has no table
    public int? DiningAreaId { get; set; }
    public int? RestaurantTableId { get; set; }
    public int? OrderId { get; set; }
    public int? ReservationId { get; set; }

    // The Tonight to-do list: a second line under the text, the one button that finishes the job,
    // and whether it goes under "Now" (orders waiting on staff) or "Next up" (bookings and pre-orders)
    public string Detail { get; set; } = string.Empty;
    public TableAction Action { get; set; } = TableAction.None;
    public bool IsNow { get; set; }
}
