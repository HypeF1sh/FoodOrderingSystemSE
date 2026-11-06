// PB11/PB18 (Wallace) - my bookings: upcoming and past
using System.Security.Claims;
using FoodOrderingSystem.Services;
using FoodOrderingSystem.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FoodOrderingSystem.Pages.Reservations;

public class IndexModel : PageModel
{
    private readonly IReservationService _reservationService;

    public IndexModel(IReservationService reservationService)
    {
        _reservationService = reservationService;
    }

    public List<ReservationDetailViewModel> Reservations { get; set; } = new();

    // GET: all my bookings
    public async Task OnGetAsync()
    {
        var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        Reservations = await _reservationService.GetCustomerReservationsAsync(customerId);
    }

    // POST: cancel one of my bookings (service checks it is mine and not too late)
    public async Task<IActionResult> OnPostCancelAsync(int id)
    {
        var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var (success, message) = await _reservationService.CancelReservationAsync(id, customerId);

        if (success)
            TempData["SuccessMessage"] = message;
        else
            TempData["ErrorMessage"] = message;

        return RedirectToPage();
    }
}