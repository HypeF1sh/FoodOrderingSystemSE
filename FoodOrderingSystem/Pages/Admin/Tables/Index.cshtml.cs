// PB05 (Carynn) - list tables
using FoodOrderingSystem.Models;       // <-- match your project namespace
using FoodOrderingSystem.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FoodOrderingSystem.Pages.Admin.Tables;

public class IndexModel : PageModel
{
    private readonly IDiningAreaService _service;

    public IndexModel(IDiningAreaService service)
    {
        _service = service;
    }

    public List<IGrouping<string, RestaurantTable>> Groups { get; set; } = new();

    [TempData] public string? SuccessMessage { get; set; }
    [TempData] public string? ErrorMessage { get; set; }

    // GET: load what the page shows
    public async Task OnGetAsync()
    {
        var tables = await _service.GetTablesAsync();
        Groups = tables.GroupBy(t => t.DiningArea?.Name ?? "(No area)").ToList();
    }

    // POST: the Delete button
    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        // The service checks the business rules and saves
        var (success, message) = await _service.DeleteTableAsync(id);
        if (success) SuccessMessage = message; else ErrorMessage = message;
        return RedirectToPage();
    }
}
