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
    public const int PreOrderNoticeMinutes = 5;       // a Held pre-order goes on Needs you this long before its booking

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

    public async Task<(bool Success, string Message)> CompleteTableAsync(int restaurantTableId, int? reservationId)
    {
        var today = await LoadTodayAsync();

        // The party's orders: those for the booking, plus walk-in orders from the table QR (no booking).
        // A pre-order for a later booking on the same table belongs to that party, so it is left alone.
        var orders = today.Orders
            .Where(o => (reservationId.HasValue && o.ReservationId == reservationId)
                        || (o.ReservationId == null && o.RestaurantTableId == restaurantTableId))
            .ToList();

        // 1. A table can only be closed once the kitchen is done and everything served is paid for.
        //    The button is hidden in that case, but a crafted POST must still be refused here.
        //    A Held pre-order also blocks it: staff must send it to the kitchen or cancel it first.
        var blocking = orders.FirstOrDefault(o =>
            KitchenStatuses.Contains(o.Status) || IsBillDue(o) || o.Status == OrderStatus.Held);
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

        // 3. Then the booking itself (Wallace's service also sends the customer the "leave a rating" notification).
        //    A walk-in party has no booking, so completing its orders is all there is to do.
        if (!reservationId.HasValue)
        {
            return (true, "Table completed.");
        }

        return await _reservationService.UpdateStatusByStaffAsync(reservationId.Value, ReservationStatus.Completed);
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
        // Orders for one of this table's bookings, or walk-in orders sent here by the table QR
        var orders = today.Orders
            .Where(o => (o.ReservationId.HasValue && bookingIds.Contains(o.ReservationId.Value))
                        || (o.ReservationId == null && o.RestaurantTableId == table.RestaurantTableId))
            .OrderBy(o => o.CreatedAt)
            .ToList();

        var tile = new FloorTableViewModel
        {
            RestaurantTableId = table.RestaurantTableId,
            TableNumber = table.TableNumber,
            Capacity = table.Capacity,
            DiningAreaId = table.DiningAreaId
        };

        // Pre-orders are kept apart from the state rules: they are not with the kitchen yet,
        // and staff may send one at any time (a customer who arrives early, scenario 1 in the spec)
        tile.HeldOrders = orders.Where(o => o.Status == OrderStatus.Held).ToList();
        ResolveState(tile, bookings, openBookings, orders.Where(o => o.Status != OrderStatus.Held).ToList(), today.Now);
        if (tile.HeldOrders.Count > 0)
        {
            tile.Subtitle += " · pre-order";
            // CompleteTableAsync refuses while this party's pre-order is held, so do not offer the button.
            // A pre-order for a later booking on this table does not stop the current party from leaving.
            if (tile.Action == TableAction.CompleteTable
                && tile.HeldOrders.Any(o => o.ReservationId == tile.ReservationId))
            {
                tile.Action = TableAction.None;
            }
        }

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

        // 2. In use: the kitchen is working on an order, a confirmed party is at the table,
        //    or a walk-in (table QR) has eaten and paid but staff have not pressed Complete table yet.
        //    A party past its end time stays In use while it has orders, until staff press Complete table.
        var cooking = orders.FirstOrDefault(o => KitchenStatuses.Contains(o.Status));
        var seated = openBookings.FirstOrDefault(r =>
            r.Status == nameof(ReservationStatus.Confirmed)
            && Start(r) <= now
            && (now < End(r) || orders.Any(o => o.ReservationId == r.ReservationId)));
        var paidWalkIn = orders.FirstOrDefault(o => o.ReservationId == null && o.Status == OrderStatus.Served);

        if (cooking != null || seated != null || paidWalkIn != null)
        {
            tile.State = TableState.InUse;
            // A walk-in's order has no booking, so fall back to the seated party (if any) for the drawer
            SetBooking(tile, (cooking != null ? FindBooking(bookings, cooking) : null) ?? seated, orders);

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
                // seated! is safe: with no cooking order and no orders at all, seated must be what matched
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
            var where = order.TableNumber ?? "no table";
            var minutes = OrderAgeDisplay.MinutesSincePlaced(order.CreatedAt);

            var dishes = DishSummary(order);

            if (order.Status == OrderStatus.Pending && minutes > OrderWaitMinutes)
            {
                var item = OrderItem(1, "plum", $"#{order.OrderId} waiting {minutes}m · {where}", order, booking, today.Areas);
                item.HasAllergyNote = !string.IsNullOrWhiteSpace(order.Notes);
                item.Detail = $"Not started yet · {dishes}";
                item.Action = TableAction.ViewInPass;
                item.IsNow = true;
                items.Add(item);
            }
            else if (IsBillDue(order))
            {
                var label = order.TableNumber != null ? $"{order.TableNumber} bill due · #{order.OrderId}" : $"#{order.OrderId} bill due";
                var item = OrderItem(2, "plum", label, order, booking, today.Areas);
                item.Detail = $"RM {order.TotalAmount:0.00} · {dishes}";
                item.Action = TableAction.TakePayment;
                item.IsNow = true;
                items.Add(item);
            }
            else if (order.Status == OrderStatus.ReadyToServe)
            {
                var item = OrderItem(3, "jade", $"#{order.OrderId} ready · {where}", order, booking, today.Areas);
                item.Detail = dishes;
                item.Action = TableAction.MarkServed;
                item.IsNow = true;
                items.Add(item);
            }
            else if (order.Status == OrderStatus.Held && booking != null
                     && today.Now >= Start(booking).AddMinutes(-PreOrderNoticeMinutes))
            {
                // Staff check the customer has arrived (in person or by phone), then press Send to kitchen
                var item = OrderItem(3, "jade",
                    $"{booking.TableNumber} pre-order #{order.OrderId} · {booking.CustomerName} {booking.StartTime:h:mm tt}",
                    order, booking, today.Areas);
                item.SortTime = Start(booking);
                item.Detail = $"Send once they arrive · {dishes}";
                item.Action = TableAction.SendToKitchen;
                items.Add(item);
            }
        }

        // Bookings: late with no order (5), or still not confirmed and starting soon (4)
        foreach (var booking in today.Reservations.Where(IsOpenBooking))
        {
            var start = Start(booking);
            var who = $"{booking.CustomerName} {booking.StartTime:h:mm tt}";
            // A Held pre-order does not count: a late customer with a pre-order still needs a phone call
            var hasOrder = today.Orders.Any(o => o.ReservationId == booking.ReservationId && o.Status != OrderStatus.Held);

            var party = $"{booking.PartySize} guests · {booking.TableNumber}";

            if (!hasOrder && today.Now > start.AddMinutes(LateMinutes) && today.Now < End(booking))
            {
                var item = BookingItem(5, "amber", $"{who} late, no-show?", booking, start);
                item.Detail = $"{party} · call the guest or free the table";
                item.Action = TableAction.OpenTable;
                items.Add(item);
            }
            else if (booking.Status == nameof(ReservationStatus.Pending)
                     && start <= today.Now.AddMinutes(UnconfirmedWindowMinutes)
                     && today.Now < End(booking))
            {
                var item = BookingItem(4, "amber", $"{who} not confirmed", booking, start);
                item.Detail = party;
                item.Action = TableAction.ConfirmBooking;
                items.Add(item);
            }
        }

        return items.OrderBy(i => i.Priority).ThenBy(i => i.SortTime).ToList();
    }

    private static NeedsYouItemViewModel OrderItem(
        int priority, string tone, string text, StaffOrderViewModel order, ReservationDetailViewModel? booking,
        List<DiningArea> areas) =>
        new()
        {
            Priority = priority,
            Tone = tone,
            Text = text,
            SortTime = order.CreatedAt,
            OrderId = order.OrderId,
            // A walk-in order has no booking, so its own table is used, and the area is the one that table is in
            DiningAreaId = booking?.DiningAreaId
                ?? areas.FirstOrDefault(a => a.RestaurantTables.Any(t => t.RestaurantTableId == order.RestaurantTableId))?.DiningAreaId,
            RestaurantTableId = booking?.RestaurantTableId ?? order.RestaurantTableId
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
            RestaurantTableId = booking.RestaurantTableId,
            ReservationId = booking.ReservationId
        };

    // "2× Nasi lemak, 1× Teh tarik" for the to-do list's second line; long orders end in "+N more"
    private static string DishSummary(StaffOrderViewModel order)
    {
        const int shown = 2;
        var lines = order.Lines.Take(shown).Select(l => $"{l.Quantity}× {l.Name}");
        var rest = order.Lines.Count - shown;
        return string.Join(", ", lines) + (rest > 0 ? $" +{rest} more" : string.Empty);
    }

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
