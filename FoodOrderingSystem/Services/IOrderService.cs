using FoodOrderingSystem.Models.Enums;
using FoodOrderingSystem.ViewModels;

namespace FoodOrderingSystem.Services;

// Customer ordering (PB14) and staff order handling (PB15)
public interface IOrderService
{
    // ---- Customer (PB14) ----

    /// <summary>Re-checks every dish, snapshots today's prices, and saves a Pending order with its items.
    /// Returns (success, message, new order id). Nothing is saved if any dish fails the check.</summary>
    Task<(bool Success, string Message, int? OrderId)> PlaceOrderAsync(
        string customerId, List<CartSessionItem> items, string? notes);

    /// <summary>One order for the confirmation page, or null if it does not exist
    /// or belongs to a different customer.</summary>
    Task<OrderConfirmationViewModel?> GetConfirmationAsync(int orderId, string customerId);

    /// <summary>Every order the customer has placed, newest first, with its dishes (PB18).
    /// Only that customer's orders. Payment status is not included: get it from IPaymentService.</summary>
    Task<List<CustomerOrderRowViewModel>> GetCustomerOrdersAsync(string customerId);

    // ---- Staff (PB15) ----

    /// <summary>Orders placed on one day, filtered for the staff board.
    /// Active orders come oldest first (kitchen order); the other views newest first.</summary>
    Task<List<StaffOrderViewModel>> GetOrdersForStaffAsync(DateOnly date, StaffOrderFilter filter);

    /// <summary>One order for the staff cancel page, or null if it does not exist.</summary>
    Task<StaffOrderViewModel?> GetOrderForStaffAsync(int orderId);

    /// <summary>Moves an order to its next status. currentStatus is the status the staff member saw;
    /// if the order has moved on since, nothing changes. Returns (success, message).</summary>
    Task<(bool Success, string Message)> AdvanceStatusAsync(int orderId, OrderStatus currentStatus);

    /// <summary>Cancels an order that is still Pending or Preparing and has not been paid. Returns (success, message).</summary>
    Task<(bool Success, string Message)> CancelOrderAsync(int orderId, OrderStatus currentStatus);
}
