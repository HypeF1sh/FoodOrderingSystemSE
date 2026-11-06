// PB14 (Jordan) - order placed page
using System.Security.Claims;
using FoodOrderingSystem.Data;
using FoodOrderingSystem.Services;
using FoodOrderingSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FoodOrderingSystem.Pages.Orders;

[Authorize(Roles = DbSeeder.CustomerRole)]
public class ConfirmationModel : PageModel
{
    private readonly IOrderService _orderService;
    private readonly IPaymentService _paymentService;

    public ConfirmationModel(IOrderService orderService, IPaymentService paymentService)
    {
        _orderService = orderService;
        _paymentService = paymentService;
    }

    public OrderConfirmationViewModel? Order { get; set; }

    // PB16: decides between "Pay now" and "View receipt"
    public OrderPaymentSummary Payment { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        // The service only returns the order if it belongs to the logged-in customer,
        // so someone else's order number gives 404 rather than their details
        var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        Order = await _orderService.GetConfirmationAsync(id, customerId);
        if (Order == null) return NotFound();

        Payment = await _paymentService.GetPaymentSummaryAsync(id);
        return Page();
    }
}
