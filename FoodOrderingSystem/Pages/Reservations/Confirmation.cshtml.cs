// PB10 (Wallace) - booking received page
using System.Security.Claims;
using FoodOrderingSystem.Services;
using FoodOrderingSystem.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FoodOrderingSystem.Pages.Reservations;

public class ConfirmationModel : PageModel
{
    private readonly IReservationService _reservationService;

    public ConfirmationModel(IReservationService reservationService)
    {
        _reservationService = reservationService;
    }

    public ReservationDetailViewModel? Reservation { get; set; }

    // Show the booking only if it belongs to the logged-in customer
    public async Task<IActionResult> OnGetAsync(int id)
    {
        var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        Reservation = await _reservationService.GetReservationByIdAsync(id, customerId);

        if (Reservation == null)
            return NotFound();

        return Page();
    }
}