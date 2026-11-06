// PB15/PB17 (Jordan) - view model: booking picker view model
namespace FoodOrderingSystem.ViewModels;

// Data for the booking day/time/guests picker (Pages/Shared/_BookingPicker.cshtml).
// The *Name values are the form field names the page already posts (e.g. "Search.Date" or "date"),
// so the picker drops into an existing form without any PageModel change.
public record BookingPickerViewModel(
    DateOnly Date,
    TimeOnly Time,
    int PartySize,
    string DateName,
    string TimeName,
    string PartyName);
