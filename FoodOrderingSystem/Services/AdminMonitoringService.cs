// PB21 (Carynn) - admin monitoring: reservations, orders, payments in one place
using FoodOrderingSystem.Models.Enums;   // <-- match your project namespace
using FoodOrderingSystem.ViewModels;

namespace FoodOrderingSystem.Services;

public class AdminMonitoringService : IAdminMonitoringService
{
    // Same limit as the dashboard (ReportService), so a dashboard range always opens here too
    public const int MaxRangeDays = 366;

    private readonly IReservationService _reservationService;
    private readonly IOrderService _orderService;
    private readonly IPaymentService _paymentService;

    public AdminMonitoringService(
        IReservationService reservationService,
        IOrderService orderService,
        IPaymentService paymentService)
    {
        _reservationService = reservationService;
        _orderService = orderService;
        _paymentService = paymentService;
    }

    // ================= PB21: monitoring =================

    // PB21: bookings in the range, count per status, optional status filter
    public async Task<ReservationMonitorViewModel> GetReservationsAsync(DateOnly from, DateOnly to, string? status)
    {
        (from, to) = NormalizeRange(from, to);
        var all = await LoadReservationsAsync(from, to);

        var counts = Enum.GetNames<ReservationStatus>()
            .ToDictionary(
                name => name,
                name => all.Count(r => string.Equals(r.Status, name, StringComparison.OrdinalIgnoreCase)));

        // Filter by status here (not in the other service), so it works whatever format its filter expects
        var filtered = string.IsNullOrWhiteSpace(status)
            ? all
            : all.Where(r => string.Equals(r.Status, status, StringComparison.OrdinalIgnoreCase)).ToList();

        return new ReservationMonitorViewModel
        {
            Reservations = filtered.OrderBy(r => r.ReservationDate).ThenBy(r => r.StartTime).ToList(),
            TotalInRange = all.Count,
            StatusCounts = counts
        };
    }

    // PB21: orders in the range (through Jordan's IOrderService), counts and total value
    public async Task<OrderMonitorViewModel> GetOrdersAsync(DateOnly from, DateOnly to, OrderStatus? status)
    {
        (from, to) = NormalizeRange(from, to);

        // One call for all statuses; counts come from the full list, the status filter is applied here
        var all = await _orderService.GetOrdersAsync(from, to, null);

        var counts = Enum.GetValues<OrderStatus>()
            .ToDictionary(s => s.ToString(), s => all.Count(o => o.Status == s));

        var filtered = status == null ? all : all.Where(o => o.Status == status).ToList();

        return new OrderMonitorViewModel
        {
            Orders = filtered,                       // the order service already sorts newest first
            TotalInRange = all.Count,
            StatusCounts = counts,
            TotalValue = all.Where(o => o.Status != OrderStatus.Cancelled).Sum(o => o.TotalAmount)
        };
    }

    // PB21: payments in the range (through IPaymentService), paid and refunded totals
    public async Task<PaymentMonitorViewModel> GetPaymentsAsync(DateOnly from, DateOnly to, PaymentStatus? status)
    {
        (from, to) = NormalizeRange(from, to);

        var all = await _paymentService.GetPaymentsAsync(from, to, null);

        var counts = Enum.GetValues<PaymentStatus>()
            .ToDictionary(s => s.ToString(), s => all.Count(p => p.Status == s));

        var filtered = status == null ? all : all.Where(p => p.Status == status).ToList();

        return new PaymentMonitorViewModel
        {
            Payments = filtered,                     // the payment service already sorts newest first
            TotalInRange = all.Count,
            StatusCounts = counts,
            PaidTotal = all.Where(p => p.Status == PaymentStatus.Paid).Sum(p => p.Amount),
            RefundedTotal = all.Where(p => p.Status == PaymentStatus.Refunded).Sum(p => p.Amount)
        };
    }

    // ================= helpers =================

    // Swap reversed dates and cap the range at 366 days
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
