using System.ComponentModel.DataAnnotations;
using FoodOrderingSystem.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FoodOrderingSystem.Pages.Admin.Promotions;

public class EditModel : PageModel
{
    private readonly IPromotionService _service;

    public EditModel(IPromotionService service)
    {
        _service = service;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [TempData] public string? SuccessMessage { get; set; }

    public class InputModel
    {
        public int Id { get; set; }

        [Required, StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Range(typeof(decimal), "1", "100", ErrorMessage = "Discount must be between 1 and 100.")]
        [Display(Name = "Discount (%)")]
        public decimal DiscountPercent { get; set; }

        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        [Display(Name = "Start date")]
        public DateTime StartDate { get; set; }

        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        [Display(Name = "End date")]
        public DateTime EndDate { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var promo = await _service.GetAsync(id);
        if (promo == null) return NotFound();

        Input = new InputModel
        {
            Id = promo.PromotionId,
            Title = promo.Title,
            Description = promo.Description,
            DiscountPercent = promo.DiscountPercent,
            StartDate = promo.StartDate,
            EndDate = promo.EndDate,
            IsActive = promo.IsActive
        };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var (success, message) = await _service.UpdateAsync(
            Input.Id, Input.Title, Input.Description, Input.DiscountPercent, Input.StartDate, Input.EndDate, Input.IsActive);

        if (!success)
        {
            ModelState.AddModelError(string.Empty, message);
            return Page();
        }

        SuccessMessage = message;
        return RedirectToPage("Index");
    }
}
