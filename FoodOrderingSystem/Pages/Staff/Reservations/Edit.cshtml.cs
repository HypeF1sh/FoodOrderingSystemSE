// PB12 (Wallace) - staff edit a booking
using System.ComponentModel.DataAnnotations;
using FoodOrderingSystem.Models.Enums;
using FoodOrderingSystem.Services;
using FoodOrderingSystem.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FoodOrderingSystem.Pages.Staff.Reservations;

public class EditModel : PageModel
{
    private readonly IReservationService _reservationService;

    public EditModel(IReservationService reservationService)
    {
        _reservationService = reservationService;
    }

    [BindProperty]
    public StaffEditInputModel Input { get; set; } = new();

    public ReservationDetailViewModel? ReservationInfo { get; set; }
    public List<SelectListItem> TableOptions { get; set; } = new();
    public List<SelectListItem> StatusOptions { get; set; } = new();
    public string? AvailabilityWarning { get; set; }

    public class StaffEditInputModel
    {
        [Required]
        public int ReservationId { get; set; }

        [Required, DataType(DataType.Date)]
        public DateOnly Date { get; set; }

        [Required, DataType(DataType.Time)]
        public TimeOnly StartTime { get; set; }

        [Range(1, 8, ErrorMessage = "Party size must be between 1 and 8.")]
        public int PartySize { get; set; }

        [Required]
        public int RestaurantTableId { get; set; }

        [Required]
        public ReservationStatus Status { get; set; }

        [StringLength(500)]
        public string? SpecialRequests { get; set; }
    }

    // GET: load any booking for staff to edit
    public async Task<IActionResult> OnGetAsync(
        int id,
        DateOnly? date = null,
        TimeOnly? startTime = null,
        int? partySize = null)
    {
        ReservationInfo = await _reservationService.GetReservationByIdAsync(id);
        if (ReservationInfo == null)
            return NotFound();

        Enum.TryParse<ReservationStatus>(ReservationInfo.Status, out var currentStatus);

        Input = new StaffEditInputModel
        {
            ReservationId = ReservationInfo.ReservationId,
            Date = date ?? ReservationInfo.ReservationDate,
            StartTime = startTime ?? ReservationInfo.StartTime,
            PartySize = partySize ?? ReservationInfo.PartySize,
            RestaurantTableId = ReservationInfo.RestaurantTableId,
            Status = currentStatus,
            SpecialRequests = ReservationInfo.SpecialRequests
        };

        await LoadDropdownsAsync(Input.Date, Input.StartTime, Input.PartySize, Input.ReservationId, Input.RestaurantTableId);
        return Page();
    }

    // POST: staff save; overlap and capacity still checked by the service
    public async Task<IActionResult> OnPostAsync()
    {
        ReservationInfo = await _reservationService.GetReservationByIdAsync(Input.ReservationId);
        if (ReservationInfo == null)
            return NotFound();

        if (!ModelState.IsValid)
        {
            await LoadDropdownsAsync(Input.Date, Input.StartTime, Input.PartySize, Input.ReservationId, Input.RestaurantTableId);
            return Page();
        }

        var (success, message) = await _reservationService.UpdateReservationByStaffAsync(
            Input.ReservationId,
            Input.RestaurantTableId,
            Input.Date,
            Input.StartTime,
            Input.PartySize,
            Input.Status,
            Input.SpecialRequests);

        if (!success)
        {
            ModelState.AddModelError(string.Empty, message);
            await LoadDropdownsAsync(Input.Date, Input.StartTime, Input.PartySize, Input.ReservationId, Input.RestaurantTableId);
            return Page();
        }

        TempData["SuccessMessage"] = message;
        return RedirectToPage("Index", new { FilterDate = Input.Date.ToString("yyyy-MM-dd") });
    }

    private async Task LoadDropdownsAsync(
        DateOnly date,
        TimeOnly startTime,
        int partySize,
        int excludeReservationId,
        int selectedTableId)
    {
        StatusOptions = Enum.GetNames<ReservationStatus>()
            .Select(s => new SelectListItem(s, s))
            .ToList();

        var (isOpen, hoursError) = await _reservationService.ValidateOperatingHoursAsync(date, startTime, bypassAdvanceNotice: true);
        if (!isOpen)
        {
            AvailabilityWarning = hoursError;
            TableOptions = new List<SelectListItem>();
            return;
        }

        var tables = await _reservationService.GetAvailableTablesAsync(
            date, startTime, partySize, null, excludeReservationId, bypassAdvanceNotice: true);

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