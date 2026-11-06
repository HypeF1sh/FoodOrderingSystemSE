// PB15/PB17 (Jordan) - view model: table state display
namespace FoodOrderingSystem.ViewModels;

// Words and CSS names for each table state, kept in one place like OrderStatusDisplay
public static class TableStateDisplay
{
    public static readonly TableState[] InLegendOrder =
        { TableState.Free, TableState.Booked, TableState.InUse, TableState.BillDue };

    public static string Label(TableState state) => state switch
    {
        TableState.Booked => "Booked",
        TableState.InUse => "In use",
        TableState.BillDue => "Bill due",
        _ => "Free"
    };

    // Used as the CSS class "is-<name>" in backoffice.css section 9
    public static string CssName(TableState state) => state switch
    {
        TableState.Booked => "booked",
        TableState.InUse => "inuse",
        TableState.BillDue => "billdue",
        _ => "free"
    };
}
