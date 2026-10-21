using System.ComponentModel.DataAnnotations;
using FoodOrderingSystem.Services;     // <-- match your project namespace
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FoodOrderingSystem.Pages.Admin.Settings;

public class IndexModel : PageModel
{
    private readonly IRestaurantSettingService _service;

    public IndexModel(IRestaurantSettingService service)
    {
        _service = service;
    }

    [BindProperty]
    public List<SettingInput> Settings { get; set; } = new();

    [TempData] public string? SuccessMessage { get; set; }
    [TempData] public string? ErrorMessage { get; set; }

    public class SettingInput
    {
        public int Id { get; set; }

        public string Key { get; set; } = string.Empty;   // display only, not posted

        [Required, StringLength(500)]
        public string Value { get; set; } = string.Empty;
    }

    public async Task OnGetAsync()
    {
        var all = await _service.GetAllAsync();
        Settings = all.Select(s => new SettingInput { Id = s.RestaurantSettingId, Key = s.Key, Value = s.Value }).ToList();
    }

    // "Save changes" form
    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (!ModelState.IsValid)
        {
            await FillKeysAsync();      // keys are not posted, so reload them for display
            return Page();
        }

        var values = Settings.ToDictionary(s => s.Id, s => s.Value);
        var (success, message) = await _service.UpdateValuesAsync(values);
        if (success) SuccessMessage = message; else ErrorMessage = message;
        return RedirectToPage();
    }

    // "Add setting" form (plain inputs named newKey / newValue)
    public async Task<IActionResult> OnPostAddAsync(string? newKey, string? newValue)
    {
        var (success, message) = await _service.AddAsync(newKey ?? string.Empty, newValue ?? string.Empty);
        if (success) SuccessMessage = message; else ErrorMessage = message;
        return RedirectToPage();
    }

    private async Task FillKeysAsync()
    {
        var all = await _service.GetAllAsync();
        foreach (var item in Settings)
            item.Key = all.FirstOrDefault(s => s.RestaurantSettingId == item.Id)?.Key ?? string.Empty;
    }
}
