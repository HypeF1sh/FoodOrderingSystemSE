using FoodOrderingSystem.Services;
using FoodOrderingSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FoodOrderingSystem.Pages.Menu;

// Public page: customers scan the table QR code and land here without logging in (PB08)
[AllowAnonymous]
public class IndexModel : PageModel
{
    private readonly IMenuService _menuService;
    private readonly ICartService _cartService;

    public IndexModel(IMenuService menuService, ICartService cartService)
    {
        _menuService = menuService;
        _cartService = cartService;
    }

    public MenuBrowseViewModel Menu { get; set; } = new();

    // Number of dishes in the cart, shown on the "View cart" bar (PB14)
    public int CartCount { get; set; }

    // categoryId and search come from the query string, e.g. /Menu?categoryId=2&search=steak
    public async Task OnGetAsync(int? categoryId, string? search)
    {
        // The service does the filtering (Business layer); the page only displays the result
        Menu = await _menuService.GetMenuForBrowseAsync(categoryId, search);
        CartCount = _cartService.GetItemCount(HttpContext.Session);
    }
}
