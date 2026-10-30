using FoodOrderingSystem.Models.Enums;   // <-- match your project namespace
using FoodOrderingSystem.ViewModels;

namespace FoodOrderingSystem.Services;

public class CustomerActivityService : ICustomerActivityService
{
    // The reservation service returns one day at a time, so the range is capped
    public const int MaxRangeDays = 62;

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

        foreach (var r in reservations)
        {
            if (string.Equals(r.Status, nameof(ReservationStatus.Cancelled), StringComparison.OrdinalIgnoreCase))
                continue;

            var row = RowFor(r.CustomerId, r.CustomerName, r.CustomerEmail);
            row.ReservationCount++;

            if (string.Equals(r.Status, nameof(ReservationStatus.Completed), StringComparison.OrdinalIgnoreCase))
                KeepLatest(row, r.ReservationDate);
        }

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
            KeepLatest(row, DateOnly.FromDateTime(p.PaidAt.ToLocalTime()));
        }

        var list = rows.Values.ToList();

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

    // The reservation service returns one day at a time.
    // If a "between two dates" method is added later, replace this loop with it.
    private async Task<List<ReservationDetailViewModel>> LoadReservationsAsync(DateOnly from, DateOnly to)
    {
        var all = new List<ReservationDetailViewModel>();
        for (var day = from; day <= to; day = day.AddDays(1))
        {
            all.AddRange(await _reservationService.GetStaffDailyReservationsAsync(day));
        }
        return all;
    }
}
