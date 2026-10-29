using FoodOrderingSystem.Services;
using FoodOrderingSystem.ViewModels;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FoodOrderingSystem.Pages
{
    public class IndexModel : PageModel
    {
        // Carynn's service (Domain A) - we read the hours through its interface, never the table directly
        private readonly IOperatingHoursService _operatingHoursService;

        public IndexModel(IOperatingHoursService operatingHoursService)
        {
            _operatingHoursService = operatingHoursService;
        }

        public List<OperatingHoursDayViewModel> OpeningHours { get; set; } = new();

        public async Task OnGetAsync()
        {
            var week = await _operatingHoursService.GetWeekAsync();

            // The service returns Sunday first (0); show Monday first, as people read a week
            OpeningHours = week.OrderBy(d => (d.DayOfWeek + 6) % 7).ToList();
        }
    }
}
