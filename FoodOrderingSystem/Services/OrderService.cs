using FoodOrderingSystem.Data;
using FoodOrderingSystem.Models;
using FoodOrderingSystem.Models.Enums;
using FoodOrderingSystem.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace FoodOrderingSystem.Services;

public class OrderService : IOrderService
{
    // The badge on /Notifications matches on this exact word
    private const string NotificationType = "Order";

    // An order placed this many minutes before a booking starts (or any time until it ends) is for that table
    public const int EarlyArrivalMinutes = 15;

    private readonly ApplicationDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly IReservationService _reservationService;
    private readonly IDiningAreaService _diningAreaService;
    private readonly IPromotionService _promotionService;   // promo codes


    public OrderService(
        ApplicationDbContext context,
        INotificationService notificationService,
        IReservationService reservationService,
        IDiningAreaService diningAreaService,
        IPromotionService promotionService)
    {
        _context = context;
        _notificationService = notificationService;
        _reservationService = reservationService;
        _diningAreaService = diningAreaService;
        _promotionService = promotionService;
    }

    public async Task<(bool Success, string Message, int? OrderId)> PlaceOrderAsync(
        string customerId, List<CartSessionItem> items, string? notes, int? scannedTableId, string? promoCode = null)
    {
        if (string.IsNullOrEmpty(customerId))
            return (false, "Please log in to place an order.", null);
        if (items.Count == 0)
            return (false, "Your cart is empty.", null);

        // 0. Work out the table first: an order with nowhere to go is refused before anything else
        var target = await FindOrderTargetAsync(customerId, scannedTableId, DateTime.Now);
        if (!target.CanOrder)
            return (false, target.Message, null);

        // 1. Load every dish again: the cart may be old, and prices must come from the database, not the browser
        var ids = items.Select(i => i.MenuItemId).ToList();
        var dishes = await _context.MenuItems
            .AsNoTracking()
            .Include(m => m.MenuCategory)
            .Where(m => ids.Contains(m.MenuItemId))
            .ToDictionaryAsync(m => m.MenuItemId);

        // 2. Refuse the whole order if any dish has gone, sold out, or been hidden since it was added
        var problems = new List<string>();
        foreach (var item in items)
        {
            if (!dishes.TryGetValue(item.MenuItemId, out var dish))
                problems.Add("a removed dish");
            else if (!dish.IsAvailable || dish.MenuCategory == null || !dish.MenuCategory.IsActive)
                problems.Add(dish.Name);
        }

        if (problems.Count > 0)
            return (false, $"Sorry, these are no longer available: {string.Join(", ", problems)}. "
                           + "Please remove them from your cart.", null);

        // 3. Optional promo code. The discount lowers each dish's saved price, so the order total, the payment
        //    and the receipt all agree (the schema has no separate discount column).
        Promotion? promotion = null;
        if (!string.IsNullOrWhiteSpace(promoCode))
        {
            promotion = await _promotionService.GetActiveByCodeAsync(promoCode);
            if (promotion == null)
                return (false, "That promo code is not valid, has expired, or is not active.", null);
        }

        // The promotion is written in front of the customer's notes, so staff and the receipt can see it
        var finalNotes = notes?.Trim() ?? string.Empty;
        if (promotion != null)
        {
            var promoLine = $"Promo {PromotionService.CodeFor(promotion.PromotionId)} ({promotion.Title}): "
                            + $"{promotion.DiscountPercent:0.##}% off";
            finalNotes = finalNotes.Length > 0 ? $"{promoLine} | {finalNotes}" : promoLine;
            if (finalNotes.Length > 500) finalNotes = finalNotes.Substring(0, 500);
        }

        // Build the order. UnitPrice is a snapshot, so later price changes do not alter this order.
        var order = new Order
        {
            CustomerId = customerId,
            Status = OrderStatus.Pending,
            Notes = finalNotes,
            CreatedAt = DateTime.Now   // restaurant local time, as agreed in the coding conventions
        };

        foreach (var item in items)
        {
            var price = dishes[item.MenuItemId].Price;
            if (promotion != null)
                price = PromotionService.ApplyDiscount(price, promotion.DiscountPercent); // promotion

            order.OrderItems.Add(new OrderItem
            {
                MenuItemId = item.MenuItemId,
                // Clamped again here in case the session value was tampered with
                Quantity = Math.Clamp(item.Quantity, 1, CartService.MaxQuantityPerLine),
                UnitPrice = price
            });
        }

        order.TotalAmount = order.OrderItems.Sum(oi => oi.UnitPrice * oi.Quantity);

        // Link the order to its table (and booking, if any). A pre-order waits as Held until staff release it.
        order.ReservationId = target.ReservationId;
        order.RestaurantTableId = target.RestaurantTableId;
        if (target.IsPreOrder) order.Status = OrderStatus.Held;

        // 4. One SaveChanges = one database transaction: the order and all its items are saved, or none are
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        // PB20: only after the save, so the customer is never told about an order that was not stored
        var placed = target.IsPreOrder
            ? $"Pre-order #{order.OrderId} is saved for your booking on {target.BookingStart:dd/MM/yyyy} at "
              + $"{target.BookingStart:hh:mm tt}. Staff will send it to the kitchen when you arrive."
            : $"Order #{order.OrderId} has been placed for table {target.TableNumber}.";

        await _notificationService.SendAsync(customerId, NotificationType,
            target.IsPreOrder ? placed : $"Order #{order.OrderId} has been placed and is pending.");

        return (true, placed, order.OrderId);
    }

    public async Task<OrderTargetViewModel> DescribeOrderTargetAsync(string customerId, int? scannedTableId)
    {
        var target = await FindOrderTargetAsync(customerId, scannedTableId, DateTime.Now);
        return new OrderTargetViewModel { CanOrder = target.CanOrder, Message = target.Message };
    }

    // Where an order placed 'now' goes. Checked in this order; the first rule that matches decides:
    // 1. the table QR this phone scanned (the customer is sitting there). If they also have a booking on now
    //    at that same table, the order is linked to the booking too, so it shows on that booking's bill.
    // 2. a Pending or Confirmed booking whose slot covers now (from EarlyArrivalMinutes before the start
    //    until the end): the customer is at the table.
    // 3. their soonest Confirmed booking still to come: a Held pre-order.
    // 4. otherwise the order is refused: from outside the restaurant only a confirmed booking can order.
    //    Pending bookings do not count for 3, because the restaurant has not accepted them yet.
    // Bookings are read through Wallace's IReservationService, tables through Carynn's IDiningAreaService.
    private async Task<OrderTarget> FindOrderTargetAsync(string customerId, int? scannedTableId, DateTime now)
    {
        var bookings = await _reservationService.GetCustomerReservationsAsync(customerId);

        var current = bookings
            .Where(r => (r.Status == nameof(ReservationStatus.Pending) || r.Status == nameof(ReservationStatus.Confirmed))
                        && r.ReservationDate.ToDateTime(r.StartTime).AddMinutes(-EarlyArrivalMinutes) <= now
                        && now < r.ReservationDate.ToDateTime(r.EndTime))
            .OrderBy(r => r.StartTime)
            .FirstOrDefault();

        // 1. Scanned table. A table deactivated since the scan no longer counts.
        if (scannedTableId.HasValue)
        {
            var table = await _diningAreaService.GetTableAsync(scannedTableId.Value);
            if (table != null && table.IsActive)
            {
                var sameTableBooking = current?.RestaurantTableId == table.RestaurantTableId ? current : null;
                return new OrderTarget(true, $"For table {table.TableNumber}, sent to the kitchen now.",
                    sameTableBooking?.ReservationId, table.RestaurantTableId, table.TableNumber, false, null);
            }
        }

        // 2. Booking on now
        if (current != null)
        {
            return new OrderTarget(true, $"For your table {current.TableNumber}, sent to the kitchen now.",
                current.ReservationId, current.RestaurantTableId, current.TableNumber, false, null);
        }

        // 3. Confirmed booking later: pre-order
        var upcoming = bookings
            .Where(r => r.Status == nameof(ReservationStatus.Confirmed)
                        && r.ReservationDate.ToDateTime(r.StartTime).AddMinutes(-EarlyArrivalMinutes) > now)
            .OrderBy(r => r.ReservationDate.ToDateTime(r.StartTime))
            .FirstOrDefault();
        if (upcoming != null)
        {
            var start = upcoming.ReservationDate.ToDateTime(upcoming.StartTime);
            return new OrderTarget(true,
                $"Pre-order for your booking on {start:dd/MM/yyyy} at {start:hh:mm tt} (table {upcoming.TableNumber}). "
                + "Staff send it to the kitchen when you arrive.",
                upcoming.ReservationId, upcoming.RestaurantTableId, upcoming.TableNumber, true, start);
        }

        // 4. Nowhere to send it. A booking still waiting for confirmation gets its own message.
        var waiting = bookings.Any(r => r.Status == nameof(ReservationStatus.Pending)
                                        && now < r.ReservationDate.ToDateTime(r.EndTime));
        return new OrderTarget(false,
            waiting
                ? "Your booking is not confirmed yet. You can pre-order once the restaurant confirms it, "
                  + "or scan the QR code on your table when you arrive."
                : "To order, scan the QR code on your table. Not at the restaurant? Book a table, "
                  + "and once it is confirmed you can pre-order.",
            null, null, null, false, null);
    }

    // The result of FindOrderTargetAsync. BookingStart is only set for a pre-order.
    private sealed record OrderTarget(
        bool CanOrder,
        string Message,
        int? ReservationId,
        int? RestaurantTableId,
        string? TableNumber,
        bool IsPreOrder,
        DateTime? BookingStart);

    public async Task<OrderConfirmationViewModel?> GetConfirmationAsync(int orderId, string customerId)
    {
        // Filtering by customerId means changing the id in the URL cannot show someone else's order
        return await _context.Orders
            .AsNoTracking()
            .Where(o => o.OrderId == orderId && o.CustomerId == customerId)
            .Select(o => new OrderConfirmationViewModel
            {
                OrderId = o.OrderId,
                CreatedAt = o.CreatedAt,
                Status = o.Status,
                TotalAmount = o.TotalAmount,
                Notes = o.Notes,
                Lines = o.OrderItems
                    .OrderBy(oi => oi.OrderItemId)
                    .Select(oi => new OrderLineViewModel
                    {
                        Name = oi.MenuItem != null ? oi.MenuItem.Name : "(removed dish)",
                        Quantity = oi.Quantity,
                        UnitPrice = oi.UnitPrice
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();
    }

    public async Task<List<CustomerOrderRowViewModel>> GetCustomerOrdersAsync(string customerId)
    {
        // Filtering by customerId means a customer only ever sees their own orders
        var orders = await _context.Orders
            .AsNoTracking()
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.CreatedAt)
            .ThenByDescending(o => o.OrderId)
            .Select(o => new CustomerOrderRowViewModel
            {
                OrderId = o.OrderId,
                CreatedAt = o.CreatedAt,
                Status = o.Status,
                TotalAmount = o.TotalAmount,
                Lines = o.OrderItems
                    .OrderBy(oi => oi.OrderItemId)
                    .Select(oi => new OrderLineViewModel
                    {
                        Name = oi.MenuItem != null ? oi.MenuItem.Name : "(removed dish)",
                        Quantity = oi.Quantity,
                        UnitPrice = oi.UnitPrice
                    })
                    .ToList()
            })
            .ToListAsync();

        // Same "still with the kitchen" rule as the staff board's Active filter, plus pre-orders still waiting
        orders.ForEach(o => o.IsActive = ActiveStatuses.Contains(o.Status) || o.Status == OrderStatus.Held);
        return orders;
    }

    // =====================================================================
    // Staff (PB15)
    // =====================================================================

    // Orders still being handled. Completed and Cancelled are finished.
    // Held is left out on purpose: a pre-order stays off The Pass until staff send it to the kitchen.
    private static readonly OrderStatus[] ActiveStatuses =
        { OrderStatus.Pending, OrderStatus.Preparing, OrderStatus.ReadyToServe, OrderStatus.Served };

    public async Task<List<StaffOrderViewModel>> GetOrdersForStaffAsync(DateOnly date, StaffOrderFilter filter)
    {
        // 1. One day only, from midnight to the next midnight (restaurant local time)
        var dayStart = date.ToDateTime(TimeOnly.MinValue);
        var dayEnd = dayStart.AddDays(1);

        //    A pre-order placed on an earlier day still belongs to the day of its booking
        var query = _context.Orders
            .AsNoTracking()
            .Where(o => (o.CreatedAt >= dayStart && o.CreatedAt < dayEnd)
                        || (o.Status == OrderStatus.Held && o.Reservation != null && o.Reservation.ReservationDate == date));

        // 2. Apply the board filter
        query = filter switch
        {
            StaffOrderFilter.Active => query.Where(o => ActiveStatuses.Contains(o.Status)),
            StaffOrderFilter.Completed => query.Where(o => o.Status == OrderStatus.Completed),
            StaffOrderFilter.Cancelled => query.Where(o => o.Status == OrderStatus.Cancelled),
            _ => query
        };

        // 3. The kitchen works first-in, first-out, so active orders are oldest first
        query = filter == StaffOrderFilter.Active
            ? query.OrderBy(o => o.CreatedAt)
            : query.OrderByDescending(o => o.CreatedAt);

        var orders = await ProjectToStaffView(query).ToListAsync();
        orders.ForEach(SetAllowedActions);
        return orders;
    }

    public async Task<StaffOrderViewModel?> GetOrderForStaffAsync(int orderId)
    {
        var order = await ProjectToStaffView(_context.Orders.AsNoTracking().Where(o => o.OrderId == orderId))
            .FirstOrDefaultAsync();

        if (order != null) SetAllowedActions(order);
        return order;
    }

    public async Task<(bool Success, string Message)> AdvanceStatusAsync(int orderId, OrderStatus currentStatus)
    {
        // 1. Work out the next step from the rules; Completed and Cancelled have none
        var nextStatus = GetNextStatus(currentStatus);
        if (nextStatus == null)
            return (false, $"Order #{orderId} is {OrderStatusDisplay.Label(currentStatus).ToLower()} and cannot move on.");

        // 2. Update only if the order is still in the status the staff member saw.
        //    The check and the update are one SQL statement, so two staff pressing the
        //    button at the same moment cannot move the order on twice.
        var order = _context.Orders.Where(o => o.OrderId == orderId && o.Status == currentStatus);

        // A pre-order's clock starts when it reaches the kitchen, so CreatedAt is reset on release.
        // Otherwise a pre-order placed yesterday would show "waiting 1,000m" on The Pass straight away.
        var released = currentStatus == OrderStatus.Held;
        var updated = released
            ? await order.ExecuteUpdateAsync(s => s
                .SetProperty(o => o.Status, nextStatus.Value)
                .SetProperty(o => o.CreatedAt, DateTime.Now))
            : await order.ExecuteUpdateAsync(s => s.SetProperty(o => o.Status, nextStatus.Value));

        if (updated == 0)
            return await StaleOrMissingAsync(orderId);

        // 3. PB20: tell the customer, not the staff member who pressed the button
        if (released)
        {
            await NotifyCustomerAsync(orderId, $"Your pre-order #{orderId} has been sent to the kitchen.");
            return (true, $"Pre-order #{orderId} has been sent to the kitchen.");
        }

        await NotifyCustomerAsync(orderId, $"Your order #{orderId} is now {OrderStatusDisplay.Label(nextStatus.Value).ToLower()}.");

        return (true, $"Order #{orderId} is now {OrderStatusDisplay.Label(nextStatus.Value).ToLower()}.");
    }

    public async Task<(bool Success, string Message)> CancelOrderAsync(int orderId, OrderStatus currentStatus)
    {
        // 1. Cancelling is only allowed before the food is ready (architecture rule)
        if (!CanBeCancelled(currentStatus))
            return (false, $"Order #{orderId} is {OrderStatusDisplay.Label(currentStatus).ToLower()} and can no longer be cancelled.");

        // 2. A paid order keeps the customer's money, so it must be refunded before it is cancelled (PB16)
        if (await _context.Payments.AnyAsync(p => p.OrderId == orderId && p.Status == PaymentStatus.Paid))
            return (false, $"Order #{orderId} has been paid. Refund it first.");

        // 3. Same one-statement check-and-update as AdvanceStatusAsync
        var updated = await _context.Orders
            .Where(o => o.OrderId == orderId && o.Status == currentStatus)
            .ExecuteUpdateAsync(s => s.SetProperty(o => o.Status, OrderStatus.Cancelled));

        if (updated == 0)
            return await StaleOrMissingAsync(orderId);

        // 4. PB20: tell the customer, not the staff member who pressed the button
        await NotifyCustomerAsync(orderId, $"Your order #{orderId} has been cancelled.");

        return (true, $"Order #{orderId} has been cancelled.");
    }

    // ---- Transition rules (docs/architecture.md) ----

    private static OrderStatus? GetNextStatus(OrderStatus status) => status switch
    {
        OrderStatus.Held => OrderStatus.Pending,   // staff send a pre-order to the kitchen
        OrderStatus.Pending => OrderStatus.Preparing,
        OrderStatus.Preparing => OrderStatus.ReadyToServe,
        OrderStatus.ReadyToServe => OrderStatus.Served,
        OrderStatus.Served => OrderStatus.Completed,
        _ => null   // Completed and Cancelled are final
    };

    // =====================================================================
    // Admin monitoring (for PB21 / PB23)
    // =====================================================================

    public async Task<List<StaffOrderViewModel>> GetOrdersAsync(DateOnly from, DateOnly to, OrderStatus? status)
    {
        if (from > to) return new List<StaffOrderViewModel>();

        // 1. Both days included: from midnight on 'from' up to (not including) midnight after 'to'
        var rangeStart = from.ToDateTime(TimeOnly.MinValue);
        var rangeEnd = to.ToDateTime(TimeOnly.MinValue).AddDays(1);

        var query = _context.Orders
            .AsNoTracking()
            .Where(o => o.CreatedAt >= rangeStart && o.CreatedAt < rangeEnd);

        if (status.HasValue)
            query = query.Where(o => o.Status == status.Value);

        // 2. Same projection as the staff board, but SetAllowedActions is not called: this list is read-only
        return await ProjectToStaffView(query
                .OrderByDescending(o => o.CreatedAt)
                .ThenByDescending(o => o.OrderId))
            .ToListAsync();
    }

    // =====================================================================
    // Rules shared by the methods above
    // =====================================================================

    private static bool CanBeCancelled(OrderStatus status) =>
        status == OrderStatus.Held || status == OrderStatus.Pending || status == OrderStatus.Preparing;

    // Staff actions only know the order id, so the customer is looked up from the order itself
    private async Task NotifyCustomerAsync(int orderId, string message)
    {
        var customerId = await _context.Orders
            .AsNoTracking()
            .Where(o => o.OrderId == orderId)
            .Select(o => o.CustomerId)
            .FirstOrDefaultAsync();

        if (customerId != null)
            await _notificationService.SendAsync(customerId, NotificationType, message);
    }

    private static void SetAllowedActions(StaffOrderViewModel order)
    {
        order.NextStatus = GetNextStatus(order.Status);
        order.CanCancel = CanBeCancelled(order.Status) && !order.IsPaid;
    }

    // Nothing was updated: either the order does not exist, or someone else changed it first
    private async Task<(bool Success, string Message)> StaleOrMissingAsync(int orderId)
    {
        var status = await _context.Orders
            .AsNoTracking()
            .Where(o => o.OrderId == orderId)
            .Select(o => (OrderStatus?)o.Status)
            .FirstOrDefaultAsync();

        return status == null
            ? (false, "That order could not be found.")
            : (false, $"Order #{orderId} was already updated by someone else — it is now {OrderStatusDisplay.Label(status.Value).ToLower()}.");
    }

    private static IQueryable<StaffOrderViewModel> ProjectToStaffView(IQueryable<Order> query) =>
        query.Select(o => new StaffOrderViewModel
        {
            OrderId = o.OrderId,
            CreatedAt = o.CreatedAt,
            CustomerId = o.CustomerId,
            // Display only, through the order's own Customer link; nothing in Domain A is changed
            CustomerName = o.Customer == null ? "(unknown)"
                : o.Customer.FullName != "" ? o.Customer.FullName : (o.Customer.Email ?? "(unknown)"),
            ReservationId = o.ReservationId,
            // The order's own table first (QR or booking); orders from before table QR only have the booking's
            RestaurantTableId = o.RestaurantTableId ?? (o.Reservation != null ? (int?)o.Reservation.RestaurantTableId : null),
            TableNumber = o.RestaurantTable != null ? o.RestaurantTable.TableNumber
                : o.Reservation != null && o.Reservation.RestaurantTable != null ? o.Reservation.RestaurantTable.TableNumber
                : null,
            Status = o.Status,
            TotalAmount = o.TotalAmount,
            Notes = o.Notes,
            // Same order of precedence as PaymentService: any Paid wins, then Refunded, then Failed
            PaymentStatus = o.Payments.Any(p => p.Status == PaymentStatus.Paid) ? PaymentStatus.Paid
                : o.Payments.Any(p => p.Status == PaymentStatus.Refunded) ? PaymentStatus.Refunded
                : o.Payments.Any(p => p.Status == PaymentStatus.Failed) ? PaymentStatus.Failed
                : PaymentStatus.Unpaid,
            Lines = o.OrderItems
                .OrderBy(oi => oi.OrderItemId)
                .Select(oi => new OrderLineViewModel
                {
                    Name = oi.MenuItem != null ? oi.MenuItem.Name : "(removed dish)",
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice
                })
                .ToList()
        });
}
