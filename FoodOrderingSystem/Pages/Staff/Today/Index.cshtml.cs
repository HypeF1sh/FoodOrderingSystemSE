// PB12 (Jordan) - today's run sheet for staff
using FoodOrderingSystem.Data;
using FoodOrderingSystem.Services;
using FoodOrderingSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FoodOrderingSystem.Pages.Staff.Today;

// Read-only run sheet for the shift: counts (never money), today's bookings on a timeline, and everything
// that needs attention. Every action happens on Floor or The Pass, so each rule is enforced in one place.
[Authorize(Roles = $"{DbSeeder.StaffRole},{DbSeeder.AdminRole}")]
public class IndexModel : PageModel
{
    private readonly IFloorService _floorService;

    public IndexModel(IFloorService floorService)
    {
        _floorService = floorService;
    }

    public StaffTodayViewModel Today { get; set; } = new();

    public async Task OnGetAsync()
    {
        Today = await _floorService.GetTodayAsync();
    }
}
