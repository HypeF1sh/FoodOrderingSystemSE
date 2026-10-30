using FoodOrderingSystem.Models.Enums;   // <-- match your project namespace
using FoodOrderingSystem.ViewModels;

namespace FoodOrderingSystem.Services;

public class AdminMonitoringService : IAdminMonitoringService
{
    // The reservation service returns one day at a time, so the range is capped
    public const int MaxRangeDays = 62;

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
