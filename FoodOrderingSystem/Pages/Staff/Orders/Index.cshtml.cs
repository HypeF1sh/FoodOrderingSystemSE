// PB15 (Jordan) - The Pass: kitchen order board, advance order status
using FoodOrderingSystem.Data;
using FoodOrderingSystem.Models.Enums;
using FoodOrderingSystem.Services;
using FoodOrderingSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FoodOrderingSystem.Pages.Staff.Orders;

// The Pass: the kitchen's ticket rail for today's open orders (PB15, spec 2026-11-01-staff-floor-design.md).
// History keeps the original board: any day, with the Active / Completed / Cancelled / All filters.
[Authorize(Roles = $"{DbSeeder.StaffRole},{DbSeeder.AdminRole}")]
public class IndexModel : PageModel
{
    private readonly IOrderService _orderService;

    public IndexModel(IOrderService orderService)
    {
        _orderService = orderService;
    }

    public List<StaffOrderViewModel> Orders { get; set; } = new();

    // "rail" (the default) or "history"
    [BindProperty(SupportsGet = true)]
    public string Mode { get; set; } = "rail";

    public bool IsHistory => Mode == "history";

    // Bound from the query string so the chosen day and filter show in the URL, e.g. ?mode=history&date=2026-10-22&filter=All
    [BindProperty(SupportsGet = true)]
    public DateOnly? Date { get; set; }

    [BindProperty(SupportsGet = true)]
    public StaffOrderFilter Filter { get; set; } = StaffOrderFilter.Active;

    public DateOnly SelectedDate => Date ?? DateOnly.FromDateTime(DateTime.Now);

    // In History, only today's active list refreshes itself; an old day or a finished list does not change.
    // The rail refreshes through staff-live.js instead.
    public bool AutoRefresh => IsHistory && Filter == StaffOrderFilter.Active && SelectedDate == DateOnly.FromDateTime(DateTime.Now);

    public async Task OnGetAsync()
    {
        Orders = IsHistory
            ? await _orderService.GetOrdersForStaffAsync(SelectedDate, Filter)
            : await GetRailOrdersAsync();
    }

    // Every 20 seconds staff-live.js asks for just the rail
    public async Task<PartialViewResult> OnGetRailAsync()
    {
        return Partial("_PassRail", await GetRailOrdersAsync());
    }

    // currentStatus is the status shown on the card when the button was pressed (stale-click guard)
    public async Task<IActionResult> OnPostAdvanceAsync(int id, OrderStatus currentStatus)
    {
        var (success, message) = await _orderService.AdvanceStatusAsync(id, currentStatus);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;

        // Back to the same view, day and filter
        return RedirectToPage(new { mode = Mode, date = Date?.ToString("yyyy-MM-dd"), filter = Filter });
    }

    // Today's open orders (Pending, Preparing, ReadyToServe, Served), oldest first, as the kitchen works them
    private Task<List<StaffOrderViewModel>> GetRailOrdersAsync() =>
        _orderService.GetOrdersForStaffAsync(DateOnly.FromDateTime(DateTime.Now), StaffOrderFilter.Active);
}
