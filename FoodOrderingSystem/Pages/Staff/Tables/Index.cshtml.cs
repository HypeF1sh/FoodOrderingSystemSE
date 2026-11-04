using FoodOrderingSystem.Data;
using FoodOrderingSystem.Models.Enums;
using FoodOrderingSystem.Services;
using FoodOrderingSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FoodOrderingSystem.Pages.Staff.Tables;

// Tables: the full table map with the chosen table's panel beside it (orders, bill, Take payment, Complete table).
// Same data and the same actions as Tonight's slide-in panel; every rule stays in the services.
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

    // Not called "Area": that route name is reserved for ASP.NET Core areas
    [BindProperty(SupportsGet = true)]
    public int? AreaId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? Table { get; set; }

    public FloorViewModel Floor { get; set; } = new();

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    // Every 20 seconds staff-live.js asks for just the map, so the panel being read stays still
    public async Task<PartialViewResult> OnGetMapAsync()
    {
        await LoadAsync();
        return Partial("_TablesMap", Floor);
    }

    public async Task<IActionResult> OnPostConfirmAsync(int reservationId)
    {
        var (success, message) = await _reservationService.UpdateStatusByStaffAsync(reservationId, ReservationStatus.Confirmed);
        return BackToTable(success, message);
    }

    public async Task<IActionResult> OnPostServeAsync(int orderId)
    {
        var (success, message) = await _orderService.AdvanceStatusAsync(orderId, OrderStatus.ReadyToServe);
        return BackToTable(success, message);
    }

    public async Task<IActionResult> OnPostSendToKitchenAsync(int orderId)
    {
        var (success, message) = await _orderService.AdvanceStatusAsync(orderId, OrderStatus.Held);
        return BackToTable(success, message);
    }

    // Clear the table: FloorService refuses while an order is still cooking or unpaid
    public async Task<IActionResult> OnPostCompleteAsync(int? reservationId)
    {
        if (Table == null)
        {
            return BackToTable(false, "Choose a table first.");
        }

        var (success, message) = await _floorService.CompleteTableAsync(Table.Value, reservationId);
        return BackToTable(success, message);
    }

    private async Task LoadAsync()
    {
        Floor = await _floorService.GetFloorAsync(AreaId);
        Floor.SelectedTableId = Table;
    }

    // Back to the same area with the same table open, showing the result in the usual banner
    private IActionResult BackToTable(bool success, string message)
    {
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
        return RedirectToPage(new { areaId = AreaId, table = Table });
    }
}
