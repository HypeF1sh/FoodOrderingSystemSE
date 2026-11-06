// PB23 (Carynn) - customer activity report: visits, orders, spend per customer
using FoodOrderingSystem.Models.Enums;
using FoodOrderingSystem.ViewModels;

namespace FoodOrderingSystem.Services;

public class CustomerActivityService : ICustomerActivityService
{
    // Same limit as the dashboard (ReportService), so a dashboard range always opens here too
    public const int MaxRangeDays = 366;

    private readonly IReservationService _reservationService;
    private readonly IOrderService _orderService;
    private readonly IPaymentService _paymentService;

    public CustomerActivityService(
        IReservationService reservationService,
        IOrderService orderService,
        IPaymentService paymentService)
    {
        _reservationService = reservationService;
        _orderService = orderService;
        _paymentService = paymentService;
    }

    // PB23: one row per customer: bookings, orders, money spent, last visit
    public async Task<List<CustomerActivityRowViewModel>> GetCustomerActivityAsync(DateOnly from, DateOnly to, string? sort)
    {
        (from, to) = NormalizeRange(from, to);

        var reservations = await LoadReservationsAsync(from, to);
        var orders = await _orderService.GetOrdersAsync(from, to, null);
        var payments = await _paymentService.GetPaymentsAsync(from, to, null);

        // Group by CustomerId (names are not unique)
        var rows = new Dictionary<string, CustomerActivityRowViewModel>();

        CustomerActivityRowViewModel RowFor(string customerId, string name, string? email)
        {
            if (!rows.TryGetValue(customerId, out var row))
            {
                row = new CustomerActivityRowViewModel { CustomerId = customerId };
                rows[customerId] = row;
            }
            if (string.IsNullOrWhiteSpace(row.CustomerName)) row.CustomerName = name;
            if (string.IsNullOrWhiteSpace(row.CustomerEmail)) row.CustomerEmail = email;
            return row;
        }

        void KeepLatest(CustomerActivityRowViewModel row, DateOnly day)
        {
            if (row.LastVisit == null || day > row.LastVisit) row.LastVisit = day;
        }

        // Count bookings (not cancelled); a completed one counts as a visit
        foreach (var r in reservations)
        {
            if (string.Equals(r.Status, nameof(ReservationStatus.Cancelled), StringComparison.OrdinalIgnoreCase))
                continue;

            var row = RowFor(r.CustomerId, r.CustomerName, r.CustomerEmail);
            row.ReservationCount++;

            if (string.Equals(r.Status, nameof(ReservationStatus.Completed), StringComparison.OrdinalIgnoreCase))
                KeepLatest(row, r.ReservationDate);
        }

        // Count orders (not cancelled)
        foreach (var o in orders)
        {
            if (o.Status == OrderStatus.Cancelled) continue;

            var row = RowFor(o.CustomerId, o.CustomerName, null);
            row.OrderCount++;
        }

        // A refund changes the payment's status from Paid to Refunded, so summing Paid already leaves refunds out
        foreach (var p in payments.Where(p => p.Status == PaymentStatus.Paid))
        {
            var row = RowFor(p.CustomerId, p.CustomerName, null);
            row.TotalSpent += p.Amount;
            KeepLatest(row, DateOnly.FromDateTime(p.PaidAt));
        }

        var list = rows.Values.ToList();

        // Sort by spend, or by activity (default)
        return string.Equals(sort, "spend", StringComparison.OrdinalIgnoreCase)
            ? list.OrderByDescending(r => r.TotalSpent).ThenByDescending(r => r.ActivityCount).ThenBy(r => r.CustomerName).ToList()
            : list.OrderByDescending(r => r.ActivityCount).ThenByDescending(r => r.TotalSpent).ThenBy(r => r.CustomerName).ToList();
    }

    // ================= helpers =================

    private static (DateOnly From, DateOnly To) NormalizeRange(DateOnly from, DateOnly to)
    {
        if (to < from) (from, to) = (to, from);
        if (to.DayNumber - from.DayNumber + 1 > MaxRangeDays)
            to = from.AddDays(MaxRangeDays - 1);
        return (from, to);
    }

    // One query for the whole range through the reservation service's date-range method (PB21/PB22),
    // instead of one query per day. Callers sort and group the list themselves.
    private async Task<List<ReservationDetailViewModel>> LoadReservationsAsync(DateOnly from, DateOnly to)
    {
        return await _reservationService.GetReservationsAsync(from, to);
    }
}
