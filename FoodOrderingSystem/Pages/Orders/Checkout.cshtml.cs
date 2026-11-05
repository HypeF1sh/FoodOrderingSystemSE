using System.ComponentModel.DataAnnotations;
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
    private readonly IPromotionService _promotionService;   // promo codes

    public CheckoutModel(
        ICartService cartService, 
        IOrderService orderService, 
        ITableQrService tableQrService, 
        IPromotionService promotionService)
    {
        _cartService = cartService;
        _orderService = orderService;
        _tableQrService = tableQrService;
        _promotionService = promotionService;
    }

    [BindProperty]
    public CheckoutInput Input { get; set; } = new();

    // Optional promo code, e.g. PROMO3. The service checks it again when the order is placed.
    [BindProperty]
    [StringLength(20)]
    public string? PromoCode { get; set; }

    public CartViewModel Cart { get; set; } = new();

    // Where the order will go (scanned table, the customer's table now, a pre-order), or why it cannot be placed
    public OrderTargetViewModel OrderTarget { get; set; } = new();

    // Filled in only when a valid promo code was applied (preview, nothing is saved yet)
    public string? AppliedPromoText { get; set; }
    public decimal? DiscountAmount { get; set; }
    public decimal? DiscountedTotal { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        // 1. Nothing to check out, or a sold-out dish still in the cart: back to the cart page
        Cart = await _cartService.GetCartAsync(HttpContext.Session);
        var redirect = RedirectIfCartNotReady();
        if (redirect != null) return redirect;

        await LoadOrderTargetAsync();
        return Page();
    }

    // Promo: The "Apply" button: checks the code and shows the discounted total. Nothing is saved.
    public async Task<IActionResult> OnPostApplyAsync()
    {
        Cart = await _cartService.GetCartAsync(HttpContext.Session);
        var redirect = RedirectIfCartNotReady();
        if (redirect != null) return redirect;

        await LoadOrderTargetAsync();

        if (string.IsNullOrWhiteSpace(PromoCode))
            ModelState.AddModelError(nameof(PromoCode), "Enter a promo code first.");
        else if (!await ShowPromoPreviewAsync())
            ModelState.AddModelError(nameof(PromoCode), "That promo code is not valid, has expired, or is not active.");

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
            await ShowPromoPreviewAsync(); // Promo preview
            return Page();
        }

        // 3. The service re-checks every dish, the table and the promo code, and saves the order with today's prices
        var promoCode = string.IsNullOrWhiteSpace(PromoCode) ? null : PromoCode.Trim();
        var (success, message, orderId) = await _orderService.PlaceOrderAsync(
            CustomerId, _cartService.GetItems(HttpContext.Session), Input.Notes,
            _tableQrService.GetScannedTableId(HttpContext.Session), promoCode);

        if (!success || orderId == null)
        {
            await LoadOrderTargetAsync();
            await ShowPromoPreviewAsync(); // promo preview
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

    // Works out the discounted total for the page. Uses the same rule as the order service,
    // so the preview and the saved order agree. Returns false for an empty or invalid code.
    private async Task<bool> ShowPromoPreviewAsync()
    {
        PromoCode = PromoCode?.Trim();
        if (string.IsNullOrEmpty(PromoCode)) return false;

        var promotion = await _promotionService.GetActiveByCodeAsync(PromoCode);
        if (promotion == null) return false;

        var discounted = Cart.Lines.Sum(l =>
            PromotionService.ApplyDiscount(l.LineTotal / l.Quantity, promotion.DiscountPercent) * l.Quantity);

        DiscountedTotal = discounted;
        DiscountAmount = Cart.Total - discounted;
        AppliedPromoText = $"{promotion.Title} ({promotion.DiscountPercent:0.##}% off)";
        return true;
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
