// PB15/PB17 (Jordan) - view model: table action
namespace FoodOrderingSystem.ViewModels;

// The single next-step button in a table's drawer on the Floor
public enum TableAction
{
    None,
    ConfirmBooking,   // POST: reservation Pending -> Confirmed (Wallace's service)
    MarkServed,       // POST: order ReadyToServe -> Served
    ViewInPass,       // link only: Pending and Preparing belong to the kitchen
    TakePayment,      // link to Wallace's Bill page, so cash handling is not built twice
    CompleteTable,    // POST: FloorService.CompleteTableAsync
    SendToKitchen,    // POST: pre-order Held -> Pending, once staff know the customer is here
    OpenTable         // link only: open the table's panel (e.g. a late booking: call the guest or mark no-show)
}
