// PB22 (Jordan) - view model: report grouping
namespace FoodOrderingSystem.ViewModels;

// How the dashboard groups its charts over time (PB22). A screen choice only, never stored in the database.
public enum ReportGrouping
{
    Day,
    Week,    // Monday-start weeks
    Month
}
