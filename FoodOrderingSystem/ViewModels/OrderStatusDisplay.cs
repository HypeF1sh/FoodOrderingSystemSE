using FoodOrderingSystem.Models.Enums;

namespace FoodOrderingSystem.ViewModels;

// The words shown for each order status, kept in one place so every page says the same thing
public static class OrderStatusDisplay
{
    public static string Label(OrderStatus status) => status switch
    {
        OrderStatus.ReadyToServe => "Ready to serve",
        _ => status.ToString()
    };

    // Text for the staff button that moves an order on to this status
    public static string ActionLabel(OrderStatus nextStatus) => nextStatus switch
    {
        OrderStatus.Preparing => "Start preparing",
        OrderStatus.ReadyToServe => "Mark ready to serve",
        OrderStatus.Served => "Mark served",
        OrderStatus.Completed => "Complete",
        _ => $"Mark {Label(nextStatus).ToLower()}"
    };

    // Bootstrap badge colours, as agreed in the conventions
    public static string BadgeClass(OrderStatus status) => status switch
    {
        OrderStatus.Pending => "bg-warning text-dark",
        OrderStatus.Completed => "bg-success",
        OrderStatus.Cancelled => "bg-danger",
        _ => "bg-info text-dark"   // Preparing, ReadyToServe, Served: in progress
    };
}
