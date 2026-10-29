using FoodOrderingSystem.Services;
using FoodOrderingSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FoodOrderingSystem.Pages.Orders;

// Public page: a customer can fill the cart before logging in; login is only asked for at checkout (PB14)
[AllowAnonymous]
public class CartModel : PageModel
{
    private readonly ICartService _cartService;

    public CartModel(ICartService cartService)
    {
        _cartService = cartService;
    }

    public CartViewModel Cart { get; set; } = new();

    public async Task OnGetAsync()
    {
        // Prices and availability are read fresh from the database each time the cart is shown
        Cart = await _cartService.GetCartAsync(HttpContext.Session);
    }

    // Posted from the Add buttons on /Menu. returnUrl sends the customer back to the same tab/search.
    public async Task<IActionResult> OnPostAddAsync(int menuItemId, int quantity = 1, string? returnUrl = null)
    {
        var (success, message) = await _cartService.AddItemAsync(HttpContext.Session, menuItemId, quantity);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;

        // Only redirect to our own site, so the returnUrl cannot be used to send users elsewhere
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);

        return RedirectToPage();
    }

    // The stepper and Remove handlers only touch the session, so they have nothing to await
    public IActionResult OnPostIncrease(int menuItemId) => ChangeQuantity(menuItemId, +1);

    public IActionResult OnPostDecrease(int menuItemId) => ChangeQuantity(menuItemId, -1);

    // Removing a cart line is not destructive (nothing is saved yet, the dish can be added again),
    // so it does not need a confirmation page
    public IActionResult OnPostRemove(int menuItemId)
    {
        _cartService.RemoveItem(HttpContext.Session, menuItemId);
        return RedirectToPage();
    }

    private IActionResult ChangeQuantity(int menuItemId, int delta)
    {
        var (success, message) = _cartService.ChangeQuantity(HttpContext.Session, menuItemId, delta);
        if (!success) TempData["ErrorMessage"] = message;
        return RedirectToPage();
    }
}
