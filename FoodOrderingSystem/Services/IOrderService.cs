using FoodOrderingSystem.Models.Enums;
using FoodOrderingSystem.ViewModels;

namespace FoodOrderingSystem.Services;

// Customer ordering (PB14) and staff order handling (PB15)
public interface IOrderService
{
    // ---- Customer (PB14) ----

    /// <summary>Re-checks every dish, snapshots today's prices, and saves a Pending order with its items,
    /// linked to the scanned table or the customer's booking (a Held pre-order for a confirmed booking later).
    /// scannedTableId comes from ITableQrService.GetScannedTableId. Refused when there is no table and no
    /// confirmed booking. Returns (success, message, new order id). Nothing is saved if any check fails.</summary>
    Task<(bool Success, string Message, int? OrderId)> PlaceOrderAsync(
        string customerId, List<CartSessionItem> items, string? notes, int? scannedTableId);

    /// <summary>For the checkout page: whether an order can be placed now and where it would go
    /// (scanned table, the customer's table now, or a pre-order), or why it cannot.</summary>
    Task<OrderTargetViewModel> DescribeOrderTargetAsync(string customerId, int? scannedTableId);

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

    // ---- Admin monitoring (for PB21 / PB23, Caryn) ----

    /// <summary>Orders whose CreatedAt falls between from and to (both days included), newest first,
    /// with their payment status. status null = all statuses. Read-only: NextStatus and CanCancel are
    /// left empty, so an admin page cannot show staff actions. Returns an empty list if from is after to.</summary>
    Task<List<StaffOrderViewModel>> GetOrdersAsync(DateOnly from, DateOnly to, OrderStatus? status);

    // ---- Recommendations (PB28, Carynn) ----

    /// <summary>Dish ids ordered most, by total quantity, most ordered first. Cancelled orders do not count.
    /// customerId null = the whole restaurant. since null = all time. Read-only.</summary>
    Task<List<int>> GetTopItemIdsAsync(string? customerId, DateOnly? since, int take);
}
