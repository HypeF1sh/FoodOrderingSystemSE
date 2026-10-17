using System.ComponentModel.DataAnnotations;
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
    public SearchModel Search { get; set; } = new();

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

        [Range(1, 20, ErrorMessage = "Party size must be between 1 and 20.")]
        public int PartySize { get; set; } = 2;

        public int? DiningAreaId { get; set; }
    }

    public async Task OnGetAsync(bool check = false)
    {
        await LoadDiningAreasAsync();

        if (check)
        {
            await RunAvailabilitySearchAsync();
        }
    }

    private async Task LoadDiningAreasAsync()
    {
        var areas = await _reservationService.GetDiningAreasAsync();
        DiningAreaOptions = areas
            .Select(a => new SelectListItem(a.Name, a.DiningAreaId.ToString()))
            .ToList();
        DiningAreaOptions.Insert(0, new SelectListItem("All Dining Areas", ""));
    }

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