// PB18 (Jordan) - my orders: history and live status
using System.Security.Claims;
using FoodOrderingSystem.Data;
using FoodOrderingSystem.Services;
using FoodOrderingSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FoodOrderingSystem.Pages.Orders;

// PB18: the customer's order history with live status, plus their latest reservations
[Authorize(Roles = DbSeeder.CustomerRole)]
public class IndexModel : PageModel
{
    // The full list, with modify and cancel, is Wallace's /Reservations page
    private const int ReservationsShown = 5;

    private readonly IOrderService _orderService;
    private readonly IPaymentService _paymentService;
    private readonly IReservationService _reservationService;

    public IndexModel(IOrderService orderService, IPaymentService paymentService, IReservationService reservationService)
    {
        _orderService = orderService;
        _paymentService = paymentService;
        _reservationService = reservationService;
    }

    public List<CustomerOrderRowViewModel> ActiveOrders { get; set; } = new();
    public List<CustomerOrderRowViewModel> PastOrders { get; set; } = new();
    public List<ReservationDetailViewModel> Reservations { get; set; } = new();
    public int TotalReservations { get; set; }

    public async Task OnGetAsync()
    {
        var customerId = CurrentCustomerId();

        var orders = await LoadOrdersAsync(customerId);
        ActiveOrders = orders.Where(o => o.IsActive).ToList();
        PastOrders = orders.Where(o => !o.IsActive).ToList();

        // Read through Wallace's service only; never his tables
        var reservations = await _reservationService.GetCustomerReservationsAsync(customerId);
        TotalReservations = reservations.Count;
        Reservations = reservations.Take(ReservationsShown).ToList();
    }

    // GET /Orders?handler=Status — polled every 15 seconds by wwwroot/js/my-orders.js.
    // Same [Authorize] and same customer filter as the page, so it only ever returns your own orders.
    public async Task<JsonResult> OnGetStatusAsync()
    {
        var orders = await LoadOrdersAsync(CurrentCustomerId());

        return new JsonResult(orders.Select(o => new
        {
            o.OrderId,
            o.IsActive,
            Status = OrderStatusDisplay.Label(o.Status),
            StatusClass = OrderStatusDisplay.BadgeClass(o.Status),
            Payment = o.Payment.Status.ToString(),
            PaymentClass = PaymentStatusDisplay.BadgeClass(o.Payment.Status)
        }));
    }

    // Wallace's reservation status is a string, so the colours are matched by name (same as his My Reservations page)
    public static string ReservationBadgeClass(string status) => status switch
    {
        "Confirmed" => "bg-success",
        "Pending" => "bg-warning text-dark",
        "Completed" => "bg-primary",
        "Cancelled" => "bg-secondary",
        "NoShow" => "bg-danger",
        _ => "bg-dark"
    };

    private string CurrentCustomerId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    // Orders from OrderService, payment status from PaymentService (one query for all orders)
    private async Task<List<CustomerOrderRowViewModel>> LoadOrdersAsync(string customerId)
    {
        var orders = await _orderService.GetCustomerOrdersAsync(customerId);
        var payments = await _paymentService.GetPaymentSummariesAsync(orders.Select(o => o.OrderId));

        foreach (var order in orders)
            order.Payment = payments[order.OrderId];

        return orders;
    }
}
