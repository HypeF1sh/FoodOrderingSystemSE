// PB08/PB28 (Jordan) - public menu: tabs, search, add to cart, recommended row
using System.Security.Claims;
using FoodOrderingSystem.Data;
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
    private readonly IRecommendationService _recommendationService;   // PB28

    public IndexModel(
        IMenuService menuService,
        ICartService cartService,
        IRecommendationService recommendationService)
    {
        _menuService = menuService;
        _cartService = cartService;
        _recommendationService = recommendationService;
    }

    public MenuBrowseViewModel Menu { get; set; } = new();

    // Number of dishes in the cart, shown on the "View cart" bar (PB14)
    public int CartCount { get; set; }

    // PB28: "Recommended for you" / "Popular right now". Empty for guests and when there is nothing to show.
    public RecommendationViewModel Recommendations { get; set; } = new();

    // categoryId and search come from the query string, e.g. /Menu?categoryId=2&search=steak
    public async Task OnGetAsync(int? categoryId, string? search)
    {
        // The service does the filtering (Business layer); the page only displays the result
        Menu = await _menuService.GetMenuForBrowseAsync(categoryId, search);
        CartCount = _cartService.GetItemCount(HttpContext.Session);

        // PB28: only for a logged-in customer, and only on the main "All" view without a search,
        // so Menu above is the whole menu and no second menu query is needed
        var isCustomer = User.Identity?.IsAuthenticated == true && User.IsInRole(DbSeeder.CustomerRole);
        if (isCustomer && categoryId == null && string.IsNullOrWhiteSpace(search))
        {
            var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(customerId))
                Recommendations = await _recommendationService.GetRecommendationsAsync(customerId, Menu);
        }
    }
}
