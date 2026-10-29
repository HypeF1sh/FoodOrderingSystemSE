using FoodOrderingSystem.Data;
using FoodOrderingSystem.Models;
using FoodOrderingSystem.Models.Enums;
using FoodOrderingSystem.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace FoodOrderingSystem.Services;

public class OrderService : IOrderService
{
    private readonly ApplicationDbContext _context;

    public OrderService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<(bool Success, string Message, int? OrderId)> PlaceOrderAsync(
        string customerId, List<CartSessionItem> items, string? notes)
    {
        if (string.IsNullOrEmpty(customerId))
            return (false, "Please log in to place an order.", null);
        if (items.Count == 0)
            return (false, "Your cart is empty.", null);

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

        // 3. Build the order. UnitPrice is a snapshot, so later price changes do not alter this order.
        var order = new Order
        {
            CustomerId = customerId,
            Status = OrderStatus.Pending,
            Notes = notes?.Trim() ?? string.Empty,
            CreatedAt = DateTime.Now   // restaurant local time, as agreed in the coding conventions
        };

        foreach (var item in items)
        {
            order.OrderItems.Add(new OrderItem
            {
                MenuItemId = item.MenuItemId,
                // Clamped again here in case the session value was tampered with
                Quantity = Math.Clamp(item.Quantity, 1, CartService.MaxQuantityPerLine),
                UnitPrice = dishes[item.MenuItemId].Price
            });
        }

        order.TotalAmount = order.OrderItems.Sum(oi => oi.UnitPrice * oi.Quantity);

        // 4. One SaveChanges = one database transaction: the order and all its items are saved, or none are
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        return (true, $"Order #{order.OrderId} has been placed.", order.OrderId);
    }

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

        // Same "still with the kitchen" rule as the staff board's Active filter
        orders.ForEach(o => o.IsActive = ActiveStatuses.Contains(o.Status));
        return orders;
    }

    // =====================================================================
    // Staff (PB15)
    // =====================================================================

    // Orders still being handled. Completed and Cancelled are finished.
    private static readonly OrderStatus[] ActiveStatuses =
        { OrderStatus.Pending, OrderStatus.Preparing, OrderStatus.ReadyToServe, OrderStatus.Served };

    public async Task<List<StaffOrderViewModel>> GetOrdersForStaffAsync(DateOnly date, StaffOrderFilter filter)
    {
        // 1. One day only, from midnight to the next midnight (restaurant local time)
        var dayStart = date.ToDateTime(TimeOnly.MinValue);
        var dayEnd = dayStart.AddDays(1);

        var query = _context.Orders
            .AsNoTracking()
            .Where(o => o.CreatedAt >= dayStart && o.CreatedAt < dayEnd);

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
        var updated = await _context.Orders
            .Where(o => o.OrderId == orderId && o.Status == currentStatus)
            .ExecuteUpdateAsync(s => s.SetProperty(o => o.Status, nextStatus.Value));

        if (updated == 0)
            return await StaleOrMissingAsync(orderId);

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

        return (true, $"Order #{orderId} has been cancelled.");
    }

    // ---- Transition rules (docs/architecture.md) ----

    private static OrderStatus? GetNextStatus(OrderStatus status) => status switch
    {
        OrderStatus.Pending => OrderStatus.Preparing,
        OrderStatus.Preparing => OrderStatus.ReadyToServe,
        OrderStatus.ReadyToServe => OrderStatus.Served,
        OrderStatus.Served => OrderStatus.Completed,
        _ => null   // Completed and Cancelled are final
    };

    private static bool CanBeCancelled(OrderStatus status) =>
        status == OrderStatus.Pending || status == OrderStatus.Preparing;

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
            // Display only, through the order's own Customer link; nothing in Domain A is changed
            CustomerName = o.Customer == null ? "(unknown)"
                : o.Customer.FullName != "" ? o.Customer.FullName : (o.Customer.Email ?? "(unknown)"),
            ReservationId = o.ReservationId,
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
