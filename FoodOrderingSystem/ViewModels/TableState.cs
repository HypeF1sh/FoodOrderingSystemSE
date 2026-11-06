// PB15/PB17 (Jordan) - view model: table state
namespace FoodOrderingSystem.ViewModels;

// The one state a table is in on the staff Floor. FloorService checks them from BillDue down to Free
// and the first rule that matches wins (spec: docs/superpowers/specs/2026-11-01-staff-floor-design.md).
public enum TableState
{
    Free,
    Booked,
    InUse,
    BillDue
}
