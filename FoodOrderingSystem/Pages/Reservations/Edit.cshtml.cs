using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using FoodOrderingSystem.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FoodOrderingSystem.Pages.Reservations;

public class EditModel : PageModel
{
    private readonly IReservationService _reservationService;

    public EditModel(IReservationService reservationService)
    {
        _reservationService = reservationService;
    }

    [BindProperty]
    public EditInputModel Input { get; set; } = new();

    public List<SelectListItem> TableOptions { get; set; } = new();
    public string? AvailabilityWarning { get; set; }

    public class EditInputModel
    {
        [Required]
        public int ReservationId { get; set; }

        [Required, DataType(DataType.Date)]
        public DateOnly Date { get; set; }

        [Required, DataType(DataType.Time)]
        public TimeOnly StartTime { get; set; }

        [Range(1, 8, ErrorMessage = "Online bookings are for 1 to 8 guests. Please call us for larger groups.")]
        public int PartySize { get; set; }

        [Required(ErrorMessage = "Please select an available table.")]
        public int RestaurantTableId { get; set; }

        [StringLength(500)]
        public string? SpecialRequests { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(
        int id,
        DateOnly? date = null,
        TimeOnly? startTime = null,
        int? partySize = null)
    {
        var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var existing = await _reservationService.GetReservationByIdAsync(id, customerId);

        if (existing == null)
            return NotFound();

        if (!existing.CanModifyOrCancel)
        {
            TempData["ErrorMessage"] = existing.LockReason ?? "This reservation can no longer be modified.";
            return RedirectToPage("Index");
        }

        Input = new EditInputModel
        {
            ReservationId = existing.ReservationId,
            Date = date ?? existing.ReservationDate,
            StartTime = startTime ?? existing.StartTime,
            PartySize = partySize ?? existing.PartySize,
            RestaurantTableId = existing.RestaurantTableId,
            SpecialRequests = existing.SpecialRequests
        };

        await LoadAvailableTablesAsync(Input.Date, Input.StartTime, Input.PartySize, existing.ReservationId, existing.RestaurantTableId);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        if (!ModelState.IsValid)
        {
            await LoadAvailableTablesAsync(Input.Date, Input.StartTime, Input.PartySize, Input.ReservationId, Input.RestaurantTableId);
            return Page();
        }

        var (success, message) = await _reservationService.UpdateReservationAsync(
            Input.ReservationId,
            customerId,
            Input.RestaurantTableId,
            Input.Date,
            Input.StartTime,
            Input.PartySize,
            Input.SpecialRequests);

        if (!success)
        {
            ModelState.AddModelError(string.Empty, message);
            await LoadAvailableTablesAsync(Input.Date, Input.StartTime, Input.PartySize, Input.ReservationId, Input.RestaurantTableId);
            return Page();
        }

        TempData["SuccessMessage"] = message;
        return RedirectToPage("Index");
    }

    private async Task LoadAvailableTablesAsync(
        DateOnly date,
        TimeOnly startTime,
        int partySize,
        int excludeReservationId,
        int selectedTableId)
    {
        if (partySize < 1 || partySize > ReservationService.MaxOnlinePartySize)
        {
            AvailabilityWarning = $"Online reservations support up to {ReservationService.MaxOnlinePartySize} guests. Please call us directly for larger groups.";
            TableOptions = new List<SelectListItem>();
            return;
        }

        var (isOpen, hoursError) = await _reservationService.ValidateOperatingHoursAsync(date, startTime);
        if (!isOpen)
        {
            AvailabilityWarning = hoursError;
            TableOptions = new List<SelectListItem>();
            return;
        }

        var tables = await _reservationService.GetAvailableTablesAsync(
            date, startTime, partySize, null, excludeReservationId);

        if (tables.Count == 0)
        {
            AvailabilityWarning = "No tables are available for that date, time, and party size.";
        }

        TableOptions = tables
            .Select(t => new SelectListItem(
                $"Table {t.TableNumber} ({t.DiningAreaName} – Seats up to {t.Capacity})",
                t.RestaurantTableId.ToString(),
                t.RestaurantTableId == selectedTableId))
            .ToList();
    }
}