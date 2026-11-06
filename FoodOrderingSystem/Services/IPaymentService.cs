// PB16/PB17 (Jordan) - payments (interface)
using FoodOrderingSystem.Models.Enums;
using FoodOrderingSystem.ViewModels;

namespace FoodOrderingSystem.Services;

// Online payment and receipts (PB16), plus the billing methods staff billing (PB17, Wallace) calls.
// Nothing outside this service writes to the Payments table.
public interface IPaymentService
{
    // ---- Customer (PB16) ----

    /// <summary>The Pay page for one order, or null if it does not exist or belongs to a different customer.</summary>
    Task<PaymentPageViewModel?> GetPaymentPageAsync(int orderId, string customerId);

    /// <summary>Business checks on the typed details that data annotations cannot do (Luhn check, card not expired).
    /// Returns field name → error message; empty when everything is fine. Does not touch the database.</summary>
    Dictionary<string, string> CheckPaymentDetails(PaymentInput input);

    /// <summary>Charges the order's total through the gateway and saves one Payment row, Paid or Failed.
    /// Returns (success, message, payment id); the payment id is only set when it was Paid.</summary>
    Task<(bool Success, string Message, int? PaymentId)> PayOnlineAsync(int orderId, string customerId, PaymentInput input);

    /// <summary>Unpaid / Paid / Failed / Refunded for one order, for badges on other pages.</summary>
    Task<OrderPaymentSummary> GetPaymentSummaryAsync(int orderId);

    /// <summary>The same summary for many orders in one query, keyed by order id (PB18 "My orders").
    /// Every id asked for is in the result; an order with no payments is Unpaid.</summary>
    Task<Dictionary<int, OrderPaymentSummary>> GetPaymentSummariesAsync(IEnumerable<int> orderIds);

    // ---- Receipt (D4) ----

    /// <summary>The receipt for a Paid or Refunded payment. Customers only get their own; staff and admins any.
    /// Null if missing, Failed, or not allowed → the page shows 404.</summary>
    Task<ReceiptViewModel?> GetReceiptAsync(int paymentId, string userId, bool isStaffOrAdmin);

    // ---- Staff billing (for PB17) ----

    /// <summary>Orders placed on one day that are not cancelled and not yet paid, oldest first.</summary>
    Task<List<UnpaidOrderRowViewModel>> GetUnpaidOrdersAsync(DateOnly date);

    /// <summary>One order's bill for staff, or null if the order does not exist.</summary>
    Task<BillViewModel?> GetBillAsync(int orderId);

    /// <summary>Records a cash payment for the full order total, taken by staffUserId.
    /// Amount tendered and change are returned, not stored. To show them on the receipt, put them in
    /// TempData["CashTendered"] and TempData["CashChange"] as invariant-culture strings (e.g. "50.00")
    /// before redirecting to /Payments/Receipt/{paymentId}.</summary>
    Task<(bool Success, string Message, int? PaymentId, decimal Change)> RecordCashPaymentAsync(
        int orderId, decimal amountTendered, string staffUserId);

    /// <summary>Marks a Paid payment as Refunded (whole amount only). Staff/Admin pages only.</summary>
    Task<(bool Success, string Message)> RefundPaymentAsync(int paymentId, string staffUserId);

    // ---- Admin monitoring (for PB21 / PB23, Carynn) ----

    /// <summary>Every payment attempt whose PaidAt falls between from and to (both days included),
    /// newest first. status null = all statuses, including Failed attempts. Read-only.
    /// Returns an empty list if from is after to.</summary>
    Task<List<PaymentListItemViewModel>> GetPaymentsAsync(DateOnly from, DateOnly to, PaymentStatus? status);
}
