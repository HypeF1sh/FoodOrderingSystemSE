// PB13 (Wallace) - a customer's booking history and preferences
using FoodOrderingSystem.Services;
using FoodOrderingSystem.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FoodOrderingSystem.Pages.Staff.Reservations;

public class CustomerHistoryModel : PageModel
{
    private readonly IReservationService _reservationService;

    public CustomerHistoryModel(IReservationService reservationService)
    {
        _reservationService = reservationService;
    }

    public CustomerReservationProfileViewModel? Profile { get; set; }

    // PB13: history, preferred area, typical party size and past requests
    public async Task<IActionResult> OnGetAsync(string customerId)
    {
        if (string.IsNullOrWhiteSpace(customerId))
            return NotFound();

        Profile = await _reservationService.GetCustomerReservationProfileAsync(customerId);
        if (Profile == null)
            return NotFound();

        return Page();
    }
}