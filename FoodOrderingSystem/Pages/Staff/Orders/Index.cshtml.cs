using FoodOrderingSystem.Data;
using FoodOrderingSystem.Models.Enums;
using FoodOrderingSystem.Services;
using FoodOrderingSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FoodOrderingSystem.Pages.Staff.Orders;

// The order board staff use on an iPad or PC to receive orders and move them along (PB15)
[Authorize(Roles = $"{DbSeeder.StaffRole},{DbSeeder.AdminRole}")]
public class IndexModel : PageModel
{
    private readonly IOrderService _orderService;

    public IndexModel(IOrderService orderService)
    {
        _orderService = orderService;
    }

    public List<StaffOrderViewModel> Orders { get; set; } = new();

    // Bound from the query string so the chosen day and filter show in the URL, e.g. ?date=2026-10-22&filter=All
    [BindProperty(SupportsGet = true)]
    public DateOnly? Date { get; set; }

    [BindProperty(SupportsGet = true)]
    public StaffOrderFilter Filter { get; set; } = StaffOrderFilter.Active;

    public DateOnly SelectedDate => Date ?? DateOnly.FromDateTime(DateTime.Now);

    // Only today's active board refreshes itself; an old day or a finished list does not change
    public bool AutoRefresh => Filter == StaffOrderFilter.Active && SelectedDate == DateOnly.FromDateTime(DateTime.Now);

    public async Task OnGetAsync()
    {
        Orders = await _orderService.GetOrdersForStaffAsync(SelectedDate, Filter);
    }

    // currentStatus is the status shown on the card when the button was pressed (stale-click guard)
    public async Task<IActionResult> OnPostAdvanceAsync(int id, OrderStatus currentStatus)
    {
        var (success, message) = await _orderService.AdvanceStatusAsync(id, currentStatus);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;

        // Back to the same day and filter
        return RedirectToPage(new { date = Date?.ToString("yyyy-MM-dd"), filter = Filter });
    }
}
