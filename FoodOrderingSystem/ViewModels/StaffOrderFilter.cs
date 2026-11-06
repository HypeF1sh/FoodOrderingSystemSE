// PB15/PB17 (Jordan) - view model: staff order filter
namespace FoodOrderingSystem.ViewModels;

// Which orders the staff board shows. A screen filter only, never stored in the database.
public enum StaffOrderFilter
{
    Active,      // Pending, Preparing, ReadyToServe, Served
    Completed,
    Cancelled,
    All
}
