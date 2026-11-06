// PB09/PB10 (Wallace) - book a table: check free tables, then reserve
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using FoodOrderingSystem.Services;
using FoodOrderingSystem.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FoodOrderingSystem.Pages.Reservations;

public class CreateModel : PageModel
{
    private readonly IReservationService _reservationService;

    public CreateModel(IReservationService reservationService)
    {
        _reservationService = reservationService;
    }

    [BindProperty(SupportsGet = true)]
    // Step 1 form: date, time, guests, area (GET, so the search is in the URL)
    public SearchModel Search { get; set; } = new();

    [BindProperty]
    // Step 2 form: the table the customer picked (POST)
    public BookingInputModel Input { get; set; } = new();

    public List<SelectListItem> DiningAreaOptions { get; set; } = new();
    public List<AvailableTableViewModel> AvailableTables { get; set; } = new();
    public bool HasSearched { get; set; }
    public string? AvailabilityMessage { get; set; }

    public class SearchModel
    {
        [DataType(DataType.Date)]
        public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(1));

        [DataType(DataType.Time)]
        public TimeOnly StartTime { get; set; } = new TimeOnly(18, 0);

        [Range(1, 8, ErrorMessage = "Online bookings are for 1 to 8 guests. Please call us for larger groups.")]
        public int PartySize { get; set; } = 2;

        public int? DiningAreaId { get; set; }
    }

    public class BookingInputModel
    {
        [Required(ErrorMessage = "Please select an available table.")]
        public int RestaurantTableId { get; set; }

        [Required]
        public DateOnly Date { get; set; }

        [Required]
        public TimeOnly StartTime { get; set; }

        [Range(1, 8, ErrorMessage = "Online bookings are for 1 to 8 guests. Please call us for larger groups.")]
        public int PartySize { get; set; }

        public int? DiningAreaId { get; set; }

        [StringLength(500)]
        public string? SpecialRequests { get; set; }
    }

    // GET: show the form; with ?check=true also list free tables
    public async Task OnGetAsync(bool check = false)
    {
        await LoadDiningAreasAsync();

        if (check)
        {
            await RunAvailabilitySearchAsync();
        }
    }

    // POST: book the chosen table
    public async Task<IActionResult> OnPostAsync()
    {
        Search = new SearchModel
        {
            Date = Input.Date,
            StartTime = Input.StartTime,
            PartySize = Input.PartySize,
            DiningAreaId = Input.DiningAreaId
        };

        await LoadDiningAreasAsync();

        // 1. Field checks from the annotations
        if (!ModelState.IsValid)
        {
            await RunAvailabilitySearchAsync();
            return Page();
        }

        var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        // 2. The service re-checks hours, overlap and table availability
        var (success, message, reservationId) = await _reservationService.CreateReservationAsync(
            customerId,
            Input.RestaurantTableId,
            Input.Date,
            Input.StartTime,
            Input.PartySize,
            Input.SpecialRequests);

        // 3. Business rule failed: show the message on the same page
        if (!success || reservationId == null)
        {
            ModelState.AddModelError(string.Empty, message);
            await RunAvailabilitySearchAsync();
            return Page();
        }

        // 4. Success: Post/Redirect/Get to the confirmation page
        TempData["SuccessMessage"] = message;
        return RedirectToPage("Confirmation", new { id = reservationId.Value });
    }

    private async Task LoadDiningAreasAsync()
    {
        var areas = await _reservationService.GetDiningAreasAsync();
        DiningAreaOptions = areas
            .Select(a => new SelectListItem(a.Name, a.DiningAreaId.ToString()))
            .ToList();
        DiningAreaOptions.Insert(0, new SelectListItem("All Dining Areas", ""));
    }

    // PB09: open at that time? then which tables are free
    private async Task RunAvailabilitySearchAsync()
    {
        HasSearched = true;

        var (isOpen, hoursError) = await _reservationService.ValidateOperatingHoursAsync(Search.Date, Search.StartTime);
        if (!isOpen)
        {
            AvailabilityMessage = hoursError;
            return;
        }

        AvailableTables = await _reservationService.GetAvailableTablesAsync(
            Search.Date,
            Search.StartTime,
            Search.PartySize,
            Search.DiningAreaId);

        if (AvailableTables.Count == 0)
        {
            AvailabilityMessage = "No tables available matching your criteria for that time slot. Please try another time, party size, or dining area.";
        }
    }
}