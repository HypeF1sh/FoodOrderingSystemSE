using System.Security.Cryptography;
using FoodOrderingSystem.Data;
using FoodOrderingSystem.Models;
using FoodOrderingSystem.Models.Enums;
using FoodOrderingSystem.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace FoodOrderingSystem.Services;

public class PaymentService : IPaymentService
{
    // Same placeholder as _Layout.cshtml. To be read from Carynn's restaurant settings later (spec §10 Q4).
    public const string RestaurantName = "Rumah Rasa";

    // Typo guard for cash: staff cannot type in more than RM 1,000 over the bill
    private const decimal MaxCashOverTotal = 1000m;

    private readonly ApplicationDbContext _context;
    private readonly IPaymentGateway _gateway;

    public PaymentService(ApplicationDbContext context, IPaymentGateway gateway)
    {
        _context = context;
        _gateway = gateway;
    }

    // =====================================================================
    // Customer (PB16)
    // =====================================================================

    public async Task<PaymentPageViewModel?> GetPaymentPageAsync(int orderId, string customerId)
    {
        // Filtering by customerId means changing the id in the URL cannot show someone else's order
        var page = await _context.Orders
            .AsNoTracking()
            .Where(o => o.OrderId == orderId && o.CustomerId == customerId)
            .Select(o => new PaymentPageViewModel
            {
                OrderId = o.OrderId,
                CreatedAt = o.CreatedAt,
                OrderStatus = o.Status,
                TotalAmount = o.TotalAmount,
                Lines = o.OrderItems
                    .OrderBy(oi => oi.OrderItemId)
                    .Select(oi => new OrderLineViewModel
                    {
                        Name = oi.MenuItem != null ? oi.MenuItem.Name : "(removed dish)",
                        Quantity = oi.Quantity,
                        UnitPrice = oi.UnitPrice
                    })
                    .ToList(),
                Attempts = o.Payments
                    .OrderBy(p => p.PaymentId)
                    .Select(p => new PaymentAttemptViewModel
                    {
                        PaymentId = p.PaymentId,
                        AttemptedAt = p.PaidAt,
                        Method = p.Method,
                        Status = p.Status,
                        Amount = p.Amount,
                        TransactionRef = p.TransactionRef
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();

        if (page == null) return null;

        page.Payment = Summarise(page.Attempts);
        page.CannotPayReason = WhyCannotPay(page.OrderStatus, page.Payment.Status);
        page.CanPay = page.CannotPayReason == null;
        return page;
    }

    public Dictionary<string, string> CheckPaymentDetails(PaymentInput input)
    {
        var errors = new Dictionary<string, string>();

        if (input.Method == PaymentMethod.Card)
        {
            // 1. Luhn check: catches a mistyped card number before it ever reaches the gateway
            var digits = PaymentInput.DigitsOnly(input.CardNumber);
            if (digits.Length < 13 || digits.Length > 19 || !PassesLuhnCheck(digits))
                errors[nameof(PaymentInput.CardNumber)] = "This card number is not valid. Please check it.";

            // 2. A card is valid until the end of its expiry month
            if (!TryParseExpiry(input.Expiry, out var firstDayAfterExpiry))
                errors[nameof(PaymentInput.Expiry)] = "Use MM/YY, e.g. 08/28.";
            else if (firstDayAfterExpiry <= DateTime.Today)
                errors[nameof(PaymentInput.Expiry)] = "This card has expired.";
        }
        else if (input.Method != PaymentMethod.EWallet)
        {
            // Cash is recorded by staff at the counter, never chosen online
            errors[nameof(PaymentInput.Method)] = "Please choose Card or E-wallet.";
        }

        return errors;
    }

    public async Task<(bool Success, string Message, int? PaymentId)> PayOnlineAsync(
        int orderId, string customerId, PaymentInput input)
    {
        // 1. Check the details again here, so the rule holds even if a page forgets to
        var detailErrors = CheckPaymentDetails(input);
        if (detailErrors.Count > 0)
            return (false, detailErrors.Values.First(), null);

        // 2. One transaction from "is it still unpaid?" to "save the payment", with the order row locked,
        //    so a double-click or a second tab cannot pay the same order twice
        await using var transaction = await _context.Database.BeginTransactionAsync();
        if (!await LockOrderAsync(orderId, customerId))
            return (false, "That order could not be found.", null);

        var order = await _context.Orders
            .AsNoTracking()
            .Where(o => o.OrderId == orderId)
            .Select(o => new { o.Status, o.TotalAmount, Payments = o.Payments.Select(p => new { p.PaymentId, p.Status }).ToList() })
            .FirstAsync();

        var summary = Summarise(order.Payments.Select(p => (p.PaymentId, p.Status)));
        var reason = WhyCannotPay(order.Status, summary.Status);
        if (reason != null)
            return (false, reason, null);

        // 3. The amount is the order total from the database — the form has no amount field at all
        var (approved, reference, declineReason) = await _gateway.ChargeAsync(order.TotalAmount, input);

        // 4. Save the attempt either way: a Failed row is what lets the customer "track payment status".
        //    Only the method and the gateway's reference are stored — never the card number or CVV.
        var payment = new Payment
        {
            OrderId = orderId,
            Amount = order.TotalAmount,
            Method = input.Method,
            Status = approved ? PaymentStatus.Paid : PaymentStatus.Failed,
            TransactionRef = reference,
            PaidAt = DateTime.Now   // attempt time, restaurant local time (same as Order.CreatedAt)
        };
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return approved
            ? (true, $"Payment of RM {order.TotalAmount:0.00} received. Thank you!", payment.PaymentId)
            : (false, $"Payment failed: {declineReason} You can try again.", null);
    }

    public async Task<OrderPaymentSummary> GetPaymentSummaryAsync(int orderId)
    {
        var payments = await _context.Payments
            .AsNoTracking()
            .Where(p => p.OrderId == orderId)
            .Select(p => new { p.PaymentId, p.Status })
            .ToListAsync();

        return Summarise(payments.Select(p => (p.PaymentId, p.Status)));
    }

    // =====================================================================
    // Receipt (D4)
    // =====================================================================

    public async Task<ReceiptViewModel?> GetReceiptAsync(int paymentId, string userId, bool isStaffOrAdmin)
    {
        // Failed attempts have no receipt; customers can only open receipts for their own orders
        // (p.Order! is safe: OrderId is a required foreign key, so every payment has an order)
        return await _context.Payments
            .AsNoTracking()
            .Where(p => p.PaymentId == paymentId
                        && (p.Status == PaymentStatus.Paid || p.Status == PaymentStatus.Refunded)
                        && (isStaffOrAdmin || p.Order!.CustomerId == userId))
            .Select(p => new ReceiptViewModel
            {
                PaymentId = p.PaymentId,
                RestaurantName = RestaurantName,
                OrderId = p.OrderId,
                CustomerName = p.Order!.Customer == null ? "(unknown)"
                    : p.Order.Customer.FullName != "" ? p.Order.Customer.FullName : (p.Order.Customer.Email ?? "(unknown)"),
                PaidAt = p.PaidAt,
                Lines = p.Order.OrderItems
                    .OrderBy(oi => oi.OrderItemId)
                    .Select(oi => new OrderLineViewModel
                    {
                        Name = oi.MenuItem != null ? oi.MenuItem.Name : "(removed dish)",
                        Quantity = oi.Quantity,
                        UnitPrice = oi.UnitPrice
                    })
                    .ToList(),
                Amount = p.Amount,
                Method = p.Method,
                Status = p.Status,
                TransactionRef = p.TransactionRef,
                RecordedByName = p.RecordedByUser != null ? p.RecordedByUser.FullName : null
            })
            .FirstOrDefaultAsync();
    }

    // =====================================================================
    // Staff billing (for PB17)
    // =====================================================================

    public async Task<List<UnpaidOrderRowViewModel>> GetUnpaidOrdersAsync(DateOnly date)
    {
        var dayStart = date.ToDateTime(TimeOnly.MinValue);
        var dayEnd = dayStart.AddDays(1);

        return await _context.Orders
            .AsNoTracking()
            .Where(o => o.CreatedAt >= dayStart && o.CreatedAt < dayEnd
                        && o.Status != OrderStatus.Cancelled
                        && !o.Payments.Any(p => p.Status == PaymentStatus.Paid))
            .OrderBy(o => o.CreatedAt)
            .Select(o => new UnpaidOrderRowViewModel
            {
                OrderId = o.OrderId,
                CreatedAt = o.CreatedAt,
                CustomerName = o.Customer == null ? "(unknown)"
                    : o.Customer.FullName != "" ? o.Customer.FullName : (o.Customer.Email ?? "(unknown)"),
                ReservationId = o.ReservationId,
                OrderStatus = o.Status,
                TotalAmount = o.TotalAmount,
                HasFailedAttempt = o.Payments.Any(p => p.Status == PaymentStatus.Failed)
            })
            .ToListAsync();
    }

    public async Task<BillViewModel?> GetBillAsync(int orderId)
    {
        var bill = await _context.Orders
            .AsNoTracking()
            .Where(o => o.OrderId == orderId)
            .Select(o => new BillViewModel
            {
                OrderId = o.OrderId,
                CreatedAt = o.CreatedAt,
                CustomerName = o.Customer == null ? "(unknown)"
                    : o.Customer.FullName != "" ? o.Customer.FullName : (o.Customer.Email ?? "(unknown)"),
                OrderStatus = o.Status,
                TotalAmount = o.TotalAmount,
                Lines = o.OrderItems
                    .OrderBy(oi => oi.OrderItemId)
                    .Select(oi => new OrderLineViewModel
                    {
                        Name = oi.MenuItem != null ? oi.MenuItem.Name : "(removed dish)",
                        Quantity = oi.Quantity,
                        UnitPrice = oi.UnitPrice
                    })
                    .ToList(),
                Attempts = o.Payments
                    .OrderBy(p => p.PaymentId)
                    .Select(p => new PaymentAttemptViewModel
                    {
                        PaymentId = p.PaymentId,
                        AttemptedAt = p.PaidAt,
                        Method = p.Method,
                        Status = p.Status,
                        Amount = p.Amount,
                        TransactionRef = p.TransactionRef
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();

        if (bill == null) return null;

        bill.Payment = Summarise(bill.Attempts);
        bill.CannotPayReason = WhyCannotPay(bill.OrderStatus, bill.Payment.Status);
        bill.CanRecordPayment = bill.CannotPayReason == null;
        return bill;
    }

    public async Task<(bool Success, string Message, int? PaymentId, decimal Change)> RecordCashPaymentAsync(
        int orderId, decimal amountTendered, string staffUserId)
    {
        if (string.IsNullOrEmpty(staffUserId))
            return (false, "Please log in as staff to record a payment.", null, 0m);

        // 1. Same lock-check-save transaction as online payment, so cash and card cannot both succeed
        await using var transaction = await _context.Database.BeginTransactionAsync();
        if (!await LockOrderAsync(orderId, customerId: null))
            return (false, "That order could not be found.", null, 0m);

        var order = await _context.Orders
            .AsNoTracking()
            .Where(o => o.OrderId == orderId)
            .Select(o => new { o.Status, o.TotalAmount, Payments = o.Payments.Select(p => new { p.PaymentId, p.Status }).ToList() })
            .FirstAsync();

        var summary = Summarise(order.Payments.Select(p => (p.PaymentId, p.Status)));
        var reason = WhyCannotPay(order.Status, summary.Status);
        if (reason != null)
            return (false, reason, null, 0m);

        // 2. Enough cash, and not an obvious typo
        if (amountTendered < order.TotalAmount)
            return (false, $"Amount received must be at least the total, RM {order.TotalAmount:0.00}.", null, 0m);
        if (amountTendered > order.TotalAmount + MaxCashOverTotal)
            return (false, $"Amount received looks too high. Please check it (total is RM {order.TotalAmount:0.00}).", null, 0m);

        // 3. The payment is always the order total; tendered and change are only for the receipt
        var payment = new Payment
        {
            OrderId = orderId,
            RecordedByUserId = staffUserId,
            Amount = order.TotalAmount,
            Method = PaymentMethod.Cash,
            Status = PaymentStatus.Paid,
            TransactionRef = $"CASH-{DateTime.Now:yyyyMMdd}-{Convert.ToHexString(RandomNumberGenerator.GetBytes(4))}",
            PaidAt = DateTime.Now
        };
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        var change = amountTendered - order.TotalAmount;
        return (true, $"Cash payment of RM {order.TotalAmount:0.00} recorded. Change: RM {change:0.00}.", payment.PaymentId, change);
    }

    public async Task<(bool Success, string Message)> RefundPaymentAsync(int paymentId, string staffUserId)
    {
        if (string.IsNullOrEmpty(staffUserId))
            return (false, "Please log in as staff to refund a payment.");

        // Only a Paid payment can become Refunded. Check and update are one SQL statement,
        // so two staff pressing Refund together cannot refund it twice.
        var updated = await _context.Payments
            .Where(p => p.PaymentId == paymentId && p.Status == PaymentStatus.Paid)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Status, PaymentStatus.Refunded));

        return updated == 0
            ? (false, "Only a paid payment can be refunded.")
            : (true, $"Payment {paymentId} has been refunded.");
    }

    // =====================================================================
    // Admin monitoring (for PB21 / PB23)
    // =====================================================================

    public async Task<List<PaymentListItemViewModel>> GetPaymentsAsync(DateOnly from, DateOnly to, PaymentStatus? status)
    {
        if (from > to) return new List<PaymentListItemViewModel>();

        // 1. Both days included: from midnight on 'from' up to (not including) midnight after 'to'
        var rangeStart = from.ToDateTime(TimeOnly.MinValue);
        var rangeEnd = to.ToDateTime(TimeOnly.MinValue).AddDays(1);

        var query = _context.Payments
            .AsNoTracking()
            .Where(p => p.PaidAt >= rangeStart && p.PaidAt < rangeEnd);

        // 2. No status = every attempt, Failed ones too
        if (status.HasValue)
            query = query.Where(p => p.Status == status.Value);

        // 3. Newest first; PaymentId breaks ties when two attempts share a time
        // (p.Order! is safe: OrderId is a required foreign key, so every payment has an order)
        return await query
            .OrderByDescending(p => p.PaidAt)
            .ThenByDescending(p => p.PaymentId)
            .Select(p => new PaymentListItemViewModel
            {
                PaymentId = p.PaymentId,
                OrderId = p.OrderId,
                CustomerId = p.Order!.CustomerId,
                CustomerName = p.Order.Customer == null ? "(unknown)"
                    : p.Order.Customer.FullName != "" ? p.Order.Customer.FullName : (p.Order.Customer.Email ?? "(unknown)"),
                Amount = p.Amount,
                Method = p.Method,
                Status = p.Status,
                TransactionRef = p.TransactionRef,
                PaidAt = p.PaidAt
            })
            .ToListAsync();
    }

    // =====================================================================
    // Rules shared by the methods above
    // =====================================================================

    // The order is Paid as soon as any attempt is Paid. Otherwise Refunded, then Failed, then Unpaid.
    private static OrderPaymentSummary Summarise(IEnumerable<(int PaymentId, PaymentStatus Status)> payments)
    {
        var list = payments.ToList();
        var paid = list.FirstOrDefault(p => p.Status == PaymentStatus.Paid);

        if (paid.Status == PaymentStatus.Paid)
            return new OrderPaymentSummary { Status = PaymentStatus.Paid, PaidPaymentId = paid.PaymentId };
        if (list.Any(p => p.Status == PaymentStatus.Refunded))
            return new OrderPaymentSummary { Status = PaymentStatus.Refunded };
        if (list.Any(p => p.Status == PaymentStatus.Failed))
            return new OrderPaymentSummary { Status = PaymentStatus.Failed };

        return new OrderPaymentSummary();
    }

    private static OrderPaymentSummary Summarise(List<PaymentAttemptViewModel> attempts) =>
        Summarise(attempts.Select(a => (a.PaymentId, a.Status)));

    // Null means the order can be paid. Payment does not depend on the kitchen status (Pending → Completed).
    private static string? WhyCannotPay(OrderStatus orderStatus, PaymentStatus paymentStatus)
    {
        if (orderStatus == OrderStatus.Cancelled) return "This order was cancelled, so it cannot be paid.";
        if (paymentStatus == PaymentStatus.Paid) return "This order has already been paid.";
        if (paymentStatus == PaymentStatus.Refunded) return "This order was refunded. Please ask a member of staff.";
        return null;
    }

    // Takes a write lock on the order row until the transaction ends: the UPDATE sets Status to itself,
    // so nothing changes, but a second request for the same order waits here until the first has saved.
    // customerId null = staff, who may act on any order.
    private async Task<bool> LockOrderAsync(int orderId, string? customerId)
    {
        var locked = await _context.Orders
            .Where(o => o.OrderId == orderId && (customerId == null || o.CustomerId == customerId))
            .ExecuteUpdateAsync(s => s.SetProperty(o => o.Status, o => o.Status));
        return locked == 1;
    }

    // Luhn (mod 10): double every second digit from the right; the total must end in 0
    private static bool PassesLuhnCheck(string digits)
    {
        var sum = 0;
        var doubleIt = false;
        for (var i = digits.Length - 1; i >= 0; i--)
        {
            var d = digits[i] - '0';
            if (doubleIt)
            {
                d *= 2;
                if (d > 9) d -= 9;
            }
            sum += d;
            doubleIt = !doubleIt;
        }
        return sum % 10 == 0;
    }

    // "08/28" → 1 Sept 2028 (the first day the card no longer works)
    private static bool TryParseExpiry(string? expiry, out DateTime firstDayAfterExpiry)
    {
        firstDayAfterExpiry = default;
        if (string.IsNullOrEmpty(expiry) || expiry.Length != 5 || expiry[2] != '/') return false;
        if (!int.TryParse(expiry[..2], out var month) || !int.TryParse(expiry[3..], out var year)) return false;
        if (month < 1 || month > 12) return false;

        firstDayAfterExpiry = new DateTime(2000 + year, month, 1).AddMonths(1);
        return true;
    }
}
