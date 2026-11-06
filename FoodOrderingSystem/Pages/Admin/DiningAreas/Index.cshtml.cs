// PB05 (Carynn) - list dining areas
using FoodOrderingSystem.Models;       // <-- match your project namespace
using FoodOrderingSystem.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FoodOrderingSystem.Pages.Admin.DiningAreas;

public class IndexModel : PageModel
{
    private readonly IDiningAreaService _service;

    public IndexModel(IDiningAreaService service)
    {
        _service = service;
    }

    public List<DiningArea> Areas { get; set; } = new();

    // The layout's _Banners partial shows these keys site-wide
    [TempData] public string? SuccessMessage { get; set; }
    [TempData] public string? ErrorMessage { get; set; }

    // GET: load what the page shows
    public async Task OnGetAsync()
    {
        Areas = await _service.GetAreasAsync();
    }

    // POST: the Delete button
    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        // The service checks the business rules and saves
        var (success, message) = await _service.DeleteAreaAsync(id);
        if (success) SuccessMessage = message; else ErrorMessage = message;
        return RedirectToPage();
    }
}
