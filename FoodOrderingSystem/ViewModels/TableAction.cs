namespace FoodOrderingSystem.ViewModels;

// The single next-step button in a table's drawer on the Floor
public enum TableAction
{
    None,
    ConfirmBooking,   // POST: reservation Pending -> Confirmed (Wallace's service)
    MarkServed,       // POST: order ReadyToServe -> Served
    ViewInPass,       // link only: Pending and Preparing belong to the kitchen
    TakePayment,      // link to Wallace's Bill page, so cash handling is not built twice
    CompleteTable     // POST: FloorService.CompleteTableAsync
}
