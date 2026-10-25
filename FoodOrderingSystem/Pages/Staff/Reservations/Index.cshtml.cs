using FoodOrderingSystem.Models.Enums;
using FoodOrderingSystem.Services;
using FoodOrderingSystem.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FoodOrderingSystem.Pages.Staff.Reservations;

public class IndexModel : PageModel
{
    private readonly IReservationService _reservationService;

    public IndexModel(IReservationService reservationService)
    {
        _reservationService = reservationService;
    }

    [BindProperty(SupportsGet = true)]
    public DateOnly? FilterDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [BindProperty(SupportsGet = true)]
    public bool ShowAllDates { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? StatusFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? DiningAreaId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool OnlyWithRequests { get; set; }

    public List<ReservationDetailViewModel> Reservations { get; set; } = new();
    public List<SelectListItem> DiningAreaOptions { get; set; } = new();
    public List<SelectListItem> StatusOptions { get; set; } = new();

    public async Task OnGetAsync()
    {
        await LoadFilterOptionsAsync();

        DateOnly? effectiveDate = ShowAllDates ? null : (FilterDate ?? DateOnly.FromDateTime(DateTime.Today));

        Reservations = await _reservationService.GetStaffDailyReservationsAsync(
            effectiveDate,
            StatusFilter,
            DiningAreaId,
            SearchTerm);

        if (OnlyWithRequests)
        {
            Reservations = Reservations
                .Where(r => !string.IsNullOrWhiteSpace(r.SpecialRequests))
                .ToList();
        }
    }

    public async Task<IActionResult> OnPostUpdateStatusAsync(
        int id,
        ReservationStatus newStatus,
        DateOnly? filterDate,
        bool showAllDates,
        string? statusFilter,
        int? diningAreaId,
        string? searchTerm,
        bool onlyWithRequests)
    {
        var (success, message) = await _reservationService.UpdateStatusByStaffAsync(id, newStatus);

        if (success)
            TempData["SuccessMessage"] = message;
        else
            TempData["ErrorMessage"] = message;

        return RedirectToPage(new
        {
            FilterDate = filterDate?.ToString("yyyy-MM-dd"),
            ShowAllDates = showAllDates,
            StatusFilter = statusFilter,
            DiningAreaId = diningAreaId,
            SearchTerm = searchTerm,
            OnlyWithRequests = onlyWithRequests
        });
    }

    private async Task LoadFilterOptionsAsync()
    {
        var areas = await _reservationService.GetDiningAreasAsync();
        DiningAreaOptions = areas
            .Select(a => new SelectListItem(a.Name, a.DiningAreaId.ToString()))
            .ToList();
        DiningAreaOptions.Insert(0, new SelectListItem("All Areas", ""));

        StatusOptions = Enum.GetNames<ReservationStatus>()
            .Select(s => new SelectListItem(s, s))
            .ToList();
        StatusOptions.Insert(0, new SelectListItem("All Statuses", ""));
    }
}