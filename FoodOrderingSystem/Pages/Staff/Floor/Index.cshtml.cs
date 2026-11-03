using FoodOrderingSystem.Data;
using FoodOrderingSystem.Models.Enums;
using FoodOrderingSystem.Services;
using FoodOrderingSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FoodOrderingSystem.Pages.Staff.Floor;

// Staff landing page: every table in one dining area, a Needs-you bar on top, and a drawer for the chosen table
// (spec: docs/superpowers/specs/2026-11-01-staff-floor-design.md)
[Authorize(Roles = $"{DbSeeder.StaffRole},{DbSeeder.AdminRole}")]
public class IndexModel : PageModel
{
    private readonly IFloorService _floorService;
    private readonly IOrderService _orderService;
    private readonly IReservationService _reservationService;

    public IndexModel(IFloorService floorService, IOrderService orderService, IReservationService reservationService)
    {
        _floorService = floorService;
        _orderService = orderService;
        _reservationService = reservationService;
    }

    // Not called "Area": that route name is reserved for ASP.NET Core areas and would break every link on the page
    [BindProperty(SupportsGet = true)]
    public int? AreaId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? Table { get; set; }

    public FloorViewModel Floor { get; set; } = new();

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    // Every 20 seconds staff-live.js asks for just the Needs-you bar and the grid
    public async Task<PartialViewResult> OnGetGridAsync()
    {
        await LoadAsync();
        return Partial("_FloorGrid", Floor);
    }

    public async Task<IActionResult> OnPostConfirmAsync(int reservationId)
    {
        var (success, message) = await _reservationService.UpdateStatusByStaffAsync(reservationId, ReservationStatus.Confirmed);
        return BackToTable(success, message);
    }

    // ReadyToServe is the status the button was shown for, so a second iPad pressing it too gets a clear message
    public async Task<IActionResult> OnPostServeAsync(int orderId)
    {
        var (success, message) = await _orderService.AdvanceStatusAsync(orderId, OrderStatus.ReadyToServe);
        return BackToTable(success, message);
    }

    public async Task<IActionResult> OnPostCompleteAsync(int reservationId)
    {
        var (success, message) = await _floorService.CompleteTableAsync(reservationId);
        return BackToTable(success, message);
    }

    private async Task LoadAsync()
    {
        Floor = await _floorService.GetFloorAsync(AreaId);
        Floor.SelectedTableId = Table;
    }

    // Back to the same area with the same drawer open, showing the result in the usual banner
    private IActionResult BackToTable(bool success, string message)
    {
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
        return RedirectToPage(new { areaId = AreaId, table = Table });
    }
}
