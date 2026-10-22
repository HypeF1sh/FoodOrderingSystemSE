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

    public ConfirmationModel(IOrderService orderService)
    {
        _orderService = orderService;
    }

    public OrderConfirmationViewModel? Order { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        // The service only returns the order if it belongs to the logged-in customer,
        // so someone else's order number gives 404 rather than their details
        var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        Order = await _orderService.GetConfirmationAsync(id, customerId);
        if (Order == null) return NotFound();

        return Page();
    }
}
