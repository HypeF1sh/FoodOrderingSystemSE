using FoodOrderingSystem.Models;
using FoodOrderingSystem.Models.Enums;
using FoodOrderingSystem.ViewModels;

namespace FoodOrderingSystem.Services;

// Works out what every table is doing right now, and what needs a staff member's attention.
// It only combines what other services return, so it never touches the DbContext:
// tables from IDiningAreaService (Carynn), bookings from IReservationService (Wallace),
// orders with their payment status from IOrderService (Jordan).
public class FloorService : IFloorService
{
    // Thresholds in minutes, kept together so they are easy to find and change
    public const int BookedWindowMinutes = 60;        // a table shows Booked this long before the booking starts
    public const int OrderWaitMinutes = 10;           // a Pending order older than this goes on Needs you
    public const int UnconfirmedWindowMinutes = 120;  // a Pending booking starting within this goes on Needs you
    public const int LateMinutes = 15;                // a booking this late with no order goes on Needs you
    public const int ArrivingWindowMinutes = 120;     // "arriving in the next 2 h" on Today

    // The kitchen is still working on these
    private static readonly OrderStatus[] KitchenStatuses =
        { OrderStatus.Pending, OrderStatus.Preparing, OrderStatus.ReadyToServe };

    private readonly IDiningAreaService _diningAreaService;
    private readonly IReservationService _reservationService;
    private readonly IOrderService _orderService;

    public FloorService(
        IDiningAreaService diningAreaService,
        IReservationService reservationService,
        IOrderService orderService)
    {
        _diningAreaService = diningAreaService;
        _reservationService = reservationService;
        _orderService = orderService;
    }

    public async Task<FloorViewModel> GetFloorAsync(int? diningAreaId)
    {
        var today = await LoadTodayAsync();

        // 1. The chosen area, or the first one when none (or an unknown one) was chosen
        var selected = today.Areas.FirstOrDefault(a => a.DiningAreaId == diningAreaId) ?? today.Areas.FirstOrDefault();

        var model = new FloorViewModel
        {
            Areas = today.Areas
                .Select(a => new DiningAreaOptionViewModel
                {
                    DiningAreaId = a.DiningAreaId,
                    Name = a.Name,
                    Description = a.Description
                })
                .ToList(),
            SelectedAreaId = selected?.DiningAreaId,
            SelectedAreaName = selected?.Name ?? string.Empty,
            NeedsYou = BuildNeedsYou(today),
            UpdatedAt = today.Now
        };

        // 2. One tile per active table in that area, in table-number order (T2 before T10)
        if (selected != null)
        {
            model.Tables = selected.RestaurantTables
                .Where(t => t.IsActive)
                .OrderBy(t => t.TableNumber.Length)
                .ThenBy(t => t.TableNumber)
                .Select(t => BuildTable(t, today))
                .ToList();
        }

        return model;
    }

    public async Task<StaffTodayViewModel> GetTodayAsync()
    {
        var today = await LoadTodayAsync();
        var arrivingBy = today.Now.AddMinutes(ArrivingWindowMinutes);

        return new StaffTodayViewModel
        {
            Now = today.Now,
            BookingsToday = today.Reservations.Count,
            ArrivingSoon = today.Reservations.Count(r =>
                IsOpenBooking(r) && Start(r) >= today.Now && Start(r) <= arrivingBy),
            ActiveOrders = today.Orders.Count(o => KitchenStatuses.Contains(o.Status)),
            BillsDue = today.Orders.Count(IsBillDue),
            Timeline = today.Reservations.OrderBy(r => r.StartTime).ToList(),
            NeedsYou = BuildNeedsYou(today)
        };
    }

    public async Task<(bool Success, string Message)> CompleteTableAsync(int reservationId)
    {
        var today = await LoadTodayAsync();
        var orders = today.Orders.Where(o => o.ReservationId == reservationId).ToList();

        // 1. A table can only be closed once the kitchen is done and everything served is paid for.
        //    The button is hidden in that case, but a crafted POST must still be refused here.
        var blocking = orders.FirstOrDefault(o => KitchenStatuses.Contains(o.Status) || IsBillDue(o));
        if (blocking != null)
        {
            return (false, $"Order #{blocking.OrderId} is not finished or not paid yet, so the table can't be completed.");
        }

        // 2. Paying does not change an order's status, so paid orders are still Served: complete them first
        foreach (var order in orders.Where(o => o.Status == OrderStatus.Served))
        {
            var (success, message) = await _orderService.AdvanceStatusAsync(order.OrderId, OrderStatus.Served);
            if (!success)
            {
                return (false, message);
            }
        }

        // 3. Then the booking itself (Wallace's service also sends the customer the "leave a rating" notification)
        return await _reservationService.UpdateStatusByStaffAsync(reservationId, ReservationStatus.Completed);
    }

    // ---------------------------------------------------------------------
    // Loading
    // ---------------------------------------------------------------------

    // Everything the floor needs for today, read once per request.
    // The three calls are awaited one after another on purpose: the services share one scoped DbContext,
    // and a DbContext cannot run two queries at the same time (so no Task.WhenAll here).
    private async Task<TodaySnapshot> LoadTodayAsync()
    {
        var now = DateTime.Now;
        var date = DateOnly.FromDateTime(now);

        // GetAreasAsync sorts by name (right for the admin list); the floor keeps the order areas were set up in,
        // so the main hall, usually added first, is the default tab
        var areas = (await _diningAreaService.GetAreasAsync()).OrderBy(a => a.DiningAreaId).ToList();

        var reservations = (await _reservationService.GetStaffDailyReservationsAsync(date))
            .Where(r => r.Status != nameof(ReservationStatus.Cancelled) && r.Status != nameof(ReservationStatus.NoShow))
            .ToList();

        var orders = (await _orderService.GetOrdersForStaffAsync(date, StaffOrderFilter.All))
            .Where(o => o.Status != OrderStatus.Cancelled)
            .ToList();

        return new TodaySnapshot(now, areas, reservations, orders);
    }

    private sealed record TodaySnapshot(
        DateTime Now,
        List<DiningArea> Areas,
        List<ReservationDetailViewModel> Reservations,
        List<StaffOrderViewModel> Orders);

    // ---------------------------------------------------------------------
    // Table state: the four rules from the spec
    // ---------------------------------------------------------------------

    private static FloorTableViewModel BuildTable(RestaurantTable table, TodaySnapshot today)
    {
        var bookings = today.Reservations.Where(r => r.RestaurantTableId == table.RestaurantTableId).ToList();
        var openBookings = bookings.Where(IsOpenBooking).ToList();
        var bookingIds = bookings.Select(r => r.ReservationId).ToHashSet();
        var orders = today.Orders
            .Where(o => o.ReservationId.HasValue && bookingIds.Contains(o.ReservationId.Value))
            .OrderBy(o => o.CreatedAt)
            .ToList();

        var tile = new FloorTableViewModel
        {
            RestaurantTableId = table.RestaurantTableId,
            TableNumber = table.TableNumber,
            Capacity = table.Capacity,
            DiningAreaId = table.DiningAreaId
        };

        ResolveState(tile, bookings, openBookings, orders, today.Now);

        tile.LaterToday = openBookings
            .Where(r => r.ReservationId != tile.ReservationId && Start(r) > today.Now)
            .Select(r => $"{r.StartTime:h:mm tt} · {r.CustomerName} · {r.PartySize} pax")
            .ToList();

        return tile;
    }

    // Checked in order; the first rule that matches decides the state, the subtitle and the drawer button
    private static void ResolveState(
        FloorTableViewModel tile,
        List<ReservationDetailViewModel> bookings,
        List<ReservationDetailViewModel> openBookings,
        List<StaffOrderViewModel> orders,
        DateTime now)
    {
        // 1. Bill due: something was served and nobody has paid for it yet
        var unpaid = orders.Where(IsBillDue).ToList();
        if (unpaid.Count > 0)
        {
            tile.State = TableState.BillDue;
            tile.UnpaidTotal = unpaid.Sum(o => o.TotalAmount);
            tile.Subtitle = $"Bill due · RM {tile.UnpaidTotal:0.00}";
            tile.Action = TableAction.TakePayment;
            tile.ActionOrderId = unpaid[0].OrderId;
            SetBooking(tile, FindBooking(bookings, unpaid[0]), orders);
            return;
        }

        // 2. In use: the kitchen is working on an order, or a confirmed party is at the table.
        //    A party past its end time stays In use while it has orders, until staff press Complete table.
        var cooking = orders.FirstOrDefault(o => KitchenStatuses.Contains(o.Status));
        var seated = openBookings.FirstOrDefault(r =>
            r.Status == nameof(ReservationStatus.Confirmed)
            && Start(r) <= now
            && (now < End(r) || orders.Any(o => o.ReservationId == r.ReservationId)));

        if (cooking != null || seated != null)
        {
            tile.State = TableState.InUse;
            SetBooking(tile, cooking != null ? FindBooking(bookings, cooking) : seated, orders);

            var ready = orders.FirstOrDefault(o => o.Status == OrderStatus.ReadyToServe);
            if (ready != null)
            {
                tile.Subtitle = $"#{ready.OrderId} ready to serve";
                tile.Action = TableAction.MarkServed;
                tile.ActionOrderId = ready.OrderId;
            }
            else if (cooking != null)
            {
                var minutes = OrderAgeDisplay.MinutesSincePlaced(cooking.CreatedAt);
                tile.Subtitle = $"{OrderStatusDisplay.Label(cooking.Status)} · placed {minutes}m ago";
                tile.Action = TableAction.ViewInPass;
                tile.ActionOrderId = cooking.OrderId;
            }
            else
            {
                // seated! is safe: this branch only runs when cooking is null, so seated must be the match
                tile.Subtitle = orders.Count > 0 ? "Paid · ready to complete" : $"Seated · {seated!.PartySize} pax";
                tile.Action = TableAction.CompleteTable;
            }
            return;
        }

        // 3. Booked: a party is due within the hour, or a Pending booking's slot has already started
        var soon = openBookings.FirstOrDefault(r =>
            now < End(r) && Start(r) <= now.AddMinutes(BookedWindowMinutes));
        if (soon != null)
        {
            tile.State = TableState.Booked;
            tile.Subtitle = $"{soon.StartTime:h:mm tt} · {soon.CustomerName} · {soon.PartySize} pax";
            tile.IsPendingBooking = soon.Status == nameof(ReservationStatus.Pending);
            tile.Action = tile.IsPendingBooking ? TableAction.ConfirmBooking : TableAction.None;
            SetBooking(tile, soon, orders);
            return;
        }

        // 4. Free, mentioning the next booking later today if there is one
        var next = openBookings.FirstOrDefault(r => Start(r) > now);
        tile.State = TableState.Free;
        tile.Subtitle = next == null ? $"{tile.Capacity} seats" : $"{tile.Capacity} seats · next {next.StartTime:h:mm tt}";
    }

    private static void SetBooking(
        FloorTableViewModel tile,
        ReservationDetailViewModel? booking,
        List<StaffOrderViewModel> orders)
    {
        tile.Orders = orders.Where(o => o.Status != OrderStatus.Completed).ToList();
        if (booking == null)
        {
            return;
        }

        tile.ReservationId = booking.ReservationId;
        tile.CustomerName = booking.CustomerName;
        tile.PartySize = booking.PartySize;
        tile.StartTime = booking.StartTime;
        tile.EndTime = booking.EndTime;
    }

    // ---------------------------------------------------------------------
    // Needs you: the five triggers from the spec, most urgent first
    // ---------------------------------------------------------------------

    private static List<NeedsYouItemViewModel> BuildNeedsYou(TodaySnapshot today)
    {
        var items = new List<NeedsYouItemViewModel>();

        // Orders: waiting too long (1), bill due (2), ready to serve (3)
        foreach (var order in today.Orders)
        {
            var booking = today.Reservations.FirstOrDefault(r => r.ReservationId == order.ReservationId);
            var where = booking?.TableNumber ?? "no table";
            var minutes = OrderAgeDisplay.MinutesSincePlaced(order.CreatedAt);

            if (order.Status == OrderStatus.Pending && minutes > OrderWaitMinutes)
            {
                var item = OrderItem(1, "plum", $"#{order.OrderId} waiting {minutes}m · {where}", order, booking);
                item.HasAllergyNote = !string.IsNullOrWhiteSpace(order.Notes);
                items.Add(item);
            }
            else if (IsBillDue(order))
            {
                var label = booking != null ? $"{booking.TableNumber} bill due · #{order.OrderId}" : $"#{order.OrderId} bill due";
                items.Add(OrderItem(2, "plum", label, order, booking));
            }
            else if (order.Status == OrderStatus.ReadyToServe)
            {
                items.Add(OrderItem(3, "jade", $"#{order.OrderId} ready · {where}", order, booking));
            }
        }

        // Bookings: late with no order (5), or still not confirmed and starting soon (4)
        foreach (var booking in today.Reservations.Where(IsOpenBooking))
        {
            var start = Start(booking);
            var who = $"{booking.CustomerName} {booking.StartTime:h:mm tt}";
            var hasOrder = today.Orders.Any(o => o.ReservationId == booking.ReservationId);

            if (!hasOrder && today.Now > start.AddMinutes(LateMinutes) && today.Now < End(booking))
            {
                items.Add(BookingItem(5, "amber", $"{who} late, no-show?", booking, start));
            }
            else if (booking.Status == nameof(ReservationStatus.Pending)
                     && start <= today.Now.AddMinutes(UnconfirmedWindowMinutes)
                     && today.Now < End(booking))
            {
                items.Add(BookingItem(4, "amber", $"{who} not confirmed", booking, start));
            }
        }

        return items.OrderBy(i => i.Priority).ThenBy(i => i.SortTime).ToList();
    }

    private static NeedsYouItemViewModel OrderItem(
        int priority, string tone, string text, StaffOrderViewModel order, ReservationDetailViewModel? booking) =>
        new()
        {
            Priority = priority,
            Tone = tone,
            Text = text,
            SortTime = order.CreatedAt,
            OrderId = order.OrderId,
            DiningAreaId = booking?.DiningAreaId,
            RestaurantTableId = booking?.RestaurantTableId
        };

    private static NeedsYouItemViewModel BookingItem(
        int priority, string tone, string text, ReservationDetailViewModel booking, DateTime start) =>
        new()
        {
            Priority = priority,
            Tone = tone,
            Text = text,
            SortTime = start,
            DiningAreaId = booking.DiningAreaId,
            RestaurantTableId = booking.RestaurantTableId
        };

    // ---------------------------------------------------------------------
    // Small helpers
    // ---------------------------------------------------------------------

    // Reservation times are local DateOnly + TimeOnly. Comparing as DateTime avoids TimeOnly wrapping at midnight.
    private static DateTime Start(ReservationDetailViewModel r) => r.ReservationDate.ToDateTime(r.StartTime);
    private static DateTime End(ReservationDetailViewModel r) => r.ReservationDate.ToDateTime(r.EndTime);

    // Pending or Confirmed (Cancelled and NoShow are already filtered out when loading)
    private static bool IsOpenBooking(ReservationDetailViewModel r) =>
        r.Status == nameof(ReservationStatus.Pending) || r.Status == nameof(ReservationStatus.Confirmed);

    private static bool IsBillDue(StaffOrderViewModel o) => o.Status == OrderStatus.Served && !o.IsPaid;

    private static ReservationDetailViewModel? FindBooking(
        List<ReservationDetailViewModel> bookings, StaffOrderViewModel order) =>
        bookings.FirstOrDefault(r => r.ReservationId == order.ReservationId);
}
