using System.Security.Claims;
using FoodOrderingSystem.Data;
using FoodOrderingSystem.Services;
using FoodOrderingSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FoodOrderingSystem.Pages.Orders;

// Customers only. A logged-out visitor is sent to the login page and brought back here;
// the cart is in the session, so it is still there after logging in (PB14).
[Authorize(Roles = DbSeeder.CustomerRole)]
public class CheckoutModel : PageModel
{
    private readonly ICartService _cartService;
    private readonly IOrderService _orderService;

    public CheckoutModel(ICartService cartService, IOrderService orderService)
    {
        _cartService = cartService;
        _orderService = orderService;
    }

    [BindProperty]
    public CheckoutInput Input { get; set; } = new();

    public CartViewModel Cart { get; set; } = new();

    // Where the order will go: the customer's table now, a pre-order for their next booking, or no table
    public string OrderTarget { get; set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync()
    {
        // 1. Nothing to check out, or a sold-out dish still in the cart: back to the cart page
        Cart = await _cartService.GetCartAsync(HttpContext.Session);
        var redirect = RedirectIfCartNotReady();
        if (redirect != null) return redirect;

        OrderTarget = await _orderService.DescribeOrderTargetAsync(CustomerId);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Cart = await _cartService.GetCartAsync(HttpContext.Session);
        var redirect = RedirectIfCartNotReady();
        if (redirect != null) return redirect;

        // 2. Field checks from the data annotations (notes length)
        if (!ModelState.IsValid)
        {
            OrderTarget = await _orderService.DescribeOrderTargetAsync(CustomerId);
            return Page();
        }

        // 3. The service re-checks every dish and saves the order with today's prices
        var (success, message, orderId) = await _orderService.PlaceOrderAsync(
            CustomerId, _cartService.GetItems(HttpContext.Session), Input.Notes);

        if (!success || orderId == null)
        {
            ModelState.AddModelError(string.Empty, message);
            OrderTarget = await _orderService.DescribeOrderTargetAsync(CustomerId);
            return Page();
        }

        // 4. Only empty the cart once the order is safely saved
        _cartService.Clear(HttpContext.Session);
        TempData["SuccessMessage"] = message;
        return RedirectToPage("./Confirmation", new { id = orderId });
    }

    private string CustomerId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    private IActionResult? RedirectIfCartNotReady()
    {
        if (Cart.IsEmpty)
        {
            TempData["ErrorMessage"] = "Your cart is empty.";
            return RedirectToPage("./Cart");
        }

        if (Cart.HasUnavailableItems)
        {
            TempData["ErrorMessage"] = "Please remove the dishes that are no longer available.";
            return RedirectToPage("./Cart");
        }

        return null;
    }
}
