namespace FoodOrderingSystem.ViewModels;

// Data for the staff day stepper (Pages/Shared/_DayStepper.cshtml): the day on screen, the query-string field the
// page already reads (e.g. "FilterDate" or "date"), and any query keys to drop when the day changes
// (e.g. "ShowAllDates", so stepping to a day really shows that day).
public record DayStepperViewModel(DateOnly Date, string FieldName, params string[] DropKeys);
