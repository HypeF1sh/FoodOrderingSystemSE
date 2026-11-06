// PB22 (Jordan) - dashboard reports: sales, bookings, popular dishes
using System.Globalization;
using FoodOrderingSystem.Data;
using FoodOrderingSystem.Models;
using FoodOrderingSystem.Models.Enums;
using FoodOrderingSystem.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace FoodOrderingSystem.Services;

// Builds the admin dashboard (PB22). Reads our own Orders, OrderItems and Payments directly,
// and reservations through Wallace's IReservationService (cross-domain rule: never his table directly).
public class ReportService : IReportService
{
    // Longest range one report may cover, so a typo like year 2006 cannot load twenty years of data
    public const int MaxRangeDays = 366;

    private const int PopularItemCount = 5;

    // Up to this many days the charts show one bar per day, then per week; beyond that, per month
    private const int MaxDaysForDailyBars = 31;
    private const int MaxDaysForWeeklyBars = 92;

    private readonly ApplicationDbContext _context;
    private readonly IReservationService _reservationService;

    public ReportService(ApplicationDbContext context, IReservationService reservationService)
    {
        _context = context;
        _reservationService = reservationService;
    }

    public async Task<(bool Success, string Message, DashboardViewModel? Dashboard)> GetDashboardAsync(
        DateOnly from, DateOnly to, ReportGrouping grouping)
    {
        // 1. Check the range
        var rangeError = CheckRange(from, to);
        if (rangeError != null)
            return (false, rangeError, null);

        var (rangeStart, rangeEnd) = ToDateTimeRange(from, to);

        var dashboard = new DashboardViewModel { From = from, To = to, Grouping = grouping };

        // 2. Sales: Paid payments by payment date. A refunded payment's status becomes Refunded,
        //    so it drops out of revenue without any extra rule.
        var paidPayments = _context.Payments
            .AsNoTracking()
            .Where(p => p.Status == PaymentStatus.Paid && p.PaidAt >= rangeStart && p.PaidAt < rangeEnd);

        // The database adds up each day; C# then folds the days into weeks or months
        var salesPerDay = await paidPayments
            .GroupBy(p => p.PaidAt.Date)
            .Select(g => new { Day = g.Key, Total = g.Sum(p => p.Amount) })
            .ToListAsync();

        dashboard.Revenue = salesPerDay.Sum(d => d.Total);
        dashboard.PaidOrderCount = await paidPayments.Select(p => p.OrderId).Distinct().CountAsync();
        dashboard.AverageOrderValue = dashboard.PaidOrderCount == 0
            ? 0m
            : Math.Round(dashboard.Revenue / dashboard.PaidOrderCount, 2);
        dashboard.SalesByPeriod = GroupIntoPeriods(
            from, to, grouping,
            salesPerDay.Select(d => (DateOnly.FromDateTime(d.Day), d.Total)));

        // 3. Orders placed in the range, cancelled ones left out
        var ordersInRange = OrdersInRange(rangeStart, rangeEnd);

        dashboard.OrderCount = await ordersInRange.CountAsync();

        // 4. Popular dishes: quantity sold in those orders, top 5 (ties broken by item sales)
        dashboard.PopularItems = await SalesPerDish(ordersInRange)
            .OrderByDescending(p => p.QuantitySold)
            .ThenByDescending(p => p.ItemSales)
            .Take(PopularItemCount)
            .ToListAsync();

        // 5. Reservations through Wallace's service; the booking totals and chart leave out Cancelled
        var reservations = await _reservationService.GetReservationsAsync(from, to);
        var cancelled = ReservationStatus.Cancelled.ToString();
        var bookings = reservations.Where(r => r.Status != cancelled).ToList();

        dashboard.BookingCount = bookings.Count;
        dashboard.CoverCount = bookings.Sum(r => r.PartySize);
        dashboard.BookingsByPeriod = GroupIntoPeriods(
            from, to, grouping,
            bookings.Select(r => (r.ReservationDate, 1m)));

        return (true, string.Empty, dashboard);
    }

    public async Task<(bool Success, string Message, DishSalesViewModel? Report)> GetDishSalesAsync(DateOnly from, DateOnly to)
    {
        // 1. Same range rules as the dashboard
        var rangeError = CheckRange(from, to);
        if (rangeError != null)
            return (false, rangeError, null);

        var (rangeStart, rangeEnd) = ToDateTimeRange(from, to);

        // 2. What sold, counted the same way as the dashboard's popular dishes
        var sold = await SalesPerDish(OrdersInRange(rangeStart, rangeEnd))
            .ToDictionaryAsync(p => p.MenuItemId);

        // 3. Every dish on the menu, so the ones that sold nothing show up too
        var menu = await _context.MenuItems
            .AsNoTracking()
            .Select(m => new { m.MenuItemId, m.Name, CategoryName = m.MenuCategory!.Name, m.IsAvailable })
            .ToListAsync();

        var dishes = menu
            .Select(m => new PopularItemViewModel
            {
                MenuItemId = m.MenuItemId,
                Name = m.Name,
                CategoryName = m.CategoryName,
                IsAvailable = m.IsAvailable,
                QuantitySold = sold.TryGetValue(m.MenuItemId, out var s) ? s.QuantitySold : 0,
                ItemSales = sold.TryGetValue(m.MenuItemId, out var t) ? t.ItemSales : 0m
            })
            .OrderByDescending(d => d.QuantitySold)
            .ThenByDescending(d => d.ItemSales)
            .ThenBy(d => d.Name)
            .ToList();

        return (true, string.Empty, new DishSalesViewModel
        {
            From = from,
            To = to,
            Dishes = dishes,
            TotalQuantity = dishes.Sum(d => d.QuantitySold),
            TotalSales = dishes.Sum(d => d.ItemSales)
        });
    }

    public ReportGrouping SuggestGrouping(DateOnly from, DateOnly to)
    {
        var days = to.DayNumber - from.DayNumber + 1;
        if (days <= MaxDaysForDailyBars) return ReportGrouping.Day;
        if (days <= MaxDaysForWeeklyBars) return ReportGrouping.Week;
        return ReportGrouping.Month;
    }

    // =====================================================================
    // Shared pieces of the reports
    // =====================================================================

    // Null when the range is fine, otherwise the message to show
    private static string? CheckRange(DateOnly from, DateOnly to)
    {
        if (from > to)
            return "The start date must be on or before the end date.";

        if (to.DayNumber - from.DayNumber + 1 > MaxRangeDays)
            return $"Please choose a range of {MaxRangeDays} days or less.";

        return null;
    }

    // Both days included: from midnight on 'from' up to (not including) midnight after 'to'
    private static (DateTime Start, DateTime End) ToDateTimeRange(DateOnly from, DateOnly to) =>
        (from.ToDateTime(TimeOnly.MinValue), to.ToDateTime(TimeOnly.MinValue).AddDays(1));

    private IQueryable<Order> OrdersInRange(DateTime rangeStart, DateTime rangeEnd) => _context.Orders
        .AsNoTracking()
        .Where(o => o.CreatedAt >= rangeStart && o.CreatedAt < rangeEnd && o.Status != OrderStatus.Cancelled);

    // Quantity and item sales per dish across the given orders (still a query, so callers can sort or Take)
    private IQueryable<PopularItemViewModel> SalesPerDish(IQueryable<Order> orders) => _context.OrderItems
        .AsNoTracking()
        .Where(oi => orders.Any(o => o.OrderId == oi.OrderId))
        .GroupBy(oi => new { oi.MenuItemId, oi.MenuItem!.Name })   // MenuItemId is a required FK, so MenuItem exists
        .Select(g => new PopularItemViewModel
        {
            MenuItemId = g.Key.MenuItemId,
            Name = g.Key.Name,
            QuantitySold = g.Sum(oi => oi.Quantity),
            ItemSales = g.Sum(oi => oi.Quantity * oi.UnitPrice)
        });

    // =====================================================================
    // Periods (day / week / month) for the charts
    // =====================================================================

    // Adds each (date, amount) to its period. Every period in the range is listed, even with 0,
    // so a quiet day shows as a gap on the chart instead of disappearing.
    private static List<ReportPointViewModel> GroupIntoPeriods(
        DateOnly from, DateOnly to, ReportGrouping grouping, IEnumerable<(DateOnly Date, decimal Amount)> values)
    {
        var totals = new Dictionary<DateOnly, decimal>();
        for (var start = PeriodStart(from, grouping); start <= to; start = NextPeriod(start, grouping))
            totals[start] = 0m;

        foreach (var (date, amount) in values)
            totals[PeriodStart(date, grouping)] += amount;

        return totals
            .OrderBy(t => t.Key)
            .Select(t => new ReportPointViewModel { Label = PeriodLabel(t.Key, grouping), Value = t.Value })
            .ToList();
    }

    // First day of the period a date falls in. Weeks start on Monday, so the first week of a range
    // may begin a few days before 'from' (it still only counts data inside the range).
    private static DateOnly PeriodStart(DateOnly date, ReportGrouping grouping) => grouping switch
    {
        ReportGrouping.Week => date.AddDays(-(((int)date.DayOfWeek + 6) % 7)),   // Monday = 0 ... Sunday = 6
        ReportGrouping.Month => new DateOnly(date.Year, date.Month, 1),
        _ => date
    };

    private static DateOnly NextPeriod(DateOnly start, ReportGrouping grouping) => grouping switch
    {
        ReportGrouping.Week => start.AddDays(7),
        ReportGrouping.Month => start.AddMonths(1),
        _ => start.AddDays(1)
    };

    private static string PeriodLabel(DateOnly start, ReportGrouping grouping) => grouping switch
    {
        ReportGrouping.Week => "w/c " + start.ToString("d MMM", CultureInfo.InvariantCulture),
        ReportGrouping.Month => start.ToString("MMM yyyy", CultureInfo.InvariantCulture),
        _ => start.ToString("d MMM", CultureInfo.InvariantCulture)
    };
}
