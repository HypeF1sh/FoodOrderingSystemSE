using System.Security.Claims;
using FoodOrderingSystem.Data;
using FoodOrderingSystem.Services;
using FoodOrderingSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FoodOrderingSystem.Pages.Orders;

// Customers only. A logged-out visitor is sent to the login page and brought back here;
// the cart and the scanned table are in the session, so they are still there after logging in (PB14).
[Authorize(Roles = DbSeeder.CustomerRole)]
public class CheckoutModel : PageModel
{
    private readonly ICartService _cartService;
    private readonly IOrderService _orderService;
    private readonly ITableQrService _tableQrService;

    public CheckoutModel(ICartService cartService, IOrderService orderService, ITableQrService tableQrService)
    {
        _cartService = cartService;
        _orderService = orderService;
        _tableQrService = tableQrService;
    }

    [BindProperty]
    public CheckoutInput Input { get; set; } = new();

    public CartViewModel Cart { get; set; } = new();

    // Where the order will go (scanned table, the customer's table now, a pre-order), or why it cannot be placed
    public OrderTargetViewModel OrderTarget { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        // 1. Nothing to check out, or a sold-out dish still in the cart: back to the cart page
        Cart = await _cartService.GetCartAsync(HttpContext.Session);
        var redirect = RedirectIfCartNotReady();
        if (redirect != null) return redirect;

        await LoadOrderTargetAsync();
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
            await LoadOrderTargetAsync();
            return Page();
        }

        // 3. The service re-checks every dish and the table, and saves the order with today's prices
        var (success, message, orderId) = await _orderService.PlaceOrderAsync(
            CustomerId, _cartService.GetItems(HttpContext.Session), Input.Notes,
            _tableQrService.GetScannedTableId(HttpContext.Session));

        if (!success || orderId == null)
        {
            await LoadOrderTargetAsync();
            // "No table" is already explained in the warning box, so only other errors go in the summary
            if (OrderTarget.CanOrder) ModelState.AddModelError(string.Empty, message);
            return Page();
        }

        // 4. Only empty the cart once the order is safely saved
        _cartService.Clear(HttpContext.Session);
        TempData["SuccessMessage"] = message;
        return RedirectToPage("./Confirmation", new { id = orderId });
    }

    private string CustomerId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    private async Task LoadOrderTargetAsync()
    {
        OrderTarget = await _orderService.DescribeOrderTargetAsync(
            CustomerId, _tableQrService.GetScannedTableId(HttpContext.Session));
    }

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
