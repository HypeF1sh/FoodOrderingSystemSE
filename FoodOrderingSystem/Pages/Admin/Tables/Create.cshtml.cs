using System.ComponentModel.DataAnnotations;
using FoodOrderingSystem.Services;     // <-- match your project namespace
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FoodOrderingSystem.Pages.Admin.Tables;

public class CreateModel : PageModel
{
    private readonly IDiningAreaService _service;

    public CreateModel(IDiningAreaService service)
    {
        _service = service;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public List<SelectListItem> AreaOptions { get; set; } = new();

    [TempData] public string? SuccessMessage { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "Please choose a dining area.")]
        [Display(Name = "Dining area")]
        public int? DiningAreaId { get; set; }

        [Required, StringLength(20)]
        [Display(Name = "Table number")]
        public string TableNumber { get; set; } = string.Empty;

        [Range(1, 20, ErrorMessage = "Capacity must be between 1 and 20.")]
        public int Capacity { get; set; } = 2;

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;
    }

    public async Task OnGetAsync()
    {
        await LoadAreasAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadAreasAsync();
        if (!ModelState.IsValid) return Page();

        var (success, message) = await _service.CreateTableAsync(
            Input.DiningAreaId!.Value, Input.TableNumber, Input.Capacity, Input.IsActive);

        if (!success)
        {
            ModelState.AddModelError(string.Empty, message);
            return Page();
        }

        SuccessMessage = message;
        return RedirectToPage("Index");
    }

    private async Task LoadAreasAsync()
    {
        var areas = await _service.GetAreasAsync();
        AreaOptions = areas.Select(a => new SelectListItem(a.Name, a.DiningAreaId.ToString())).ToList();
    }
}
