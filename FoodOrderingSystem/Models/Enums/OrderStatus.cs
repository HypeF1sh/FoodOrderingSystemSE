namespace FoodOrderingSystem.Models.Enums;

public enum OrderStatus
{
    Pending,
    Preparing,
    ReadyToServe,
    Served,
    Completed,
    Cancelled,
    // A pre-order for a confirmed booking, kept off The Pass until staff send it to the kitchen.
    // Stored as a string (see ApplicationDbContext), so adding it needs no migration.
    Held
}