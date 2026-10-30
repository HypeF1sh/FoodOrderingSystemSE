using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using FoodOrderingSystem.Services;
using FoodOrderingSystem.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FoodOrderingSystem.Pages.Feedback;

public class IndexModel : PageModel
{
    private readonly IFeedbackService _feedbackService;

    public IndexModel(IFeedbackService feedbackService)
    {
        _feedbackService = feedbackService;
    }

    [BindProperty]
    public FeedbackInputModel Input { get; set; } = new();

    public List<SelectListItem> VisitOptions { get; set; } = new();
    public List<FeedbackItemViewModel> PastFeedback { get; set; } = new();

    public class FeedbackInputModel
    {
        [Required(ErrorMessage = "Please select a completed visit to rate.")]
        public string SelectedTarget { get; set; } = string.Empty; // "R:12" or "O:34"

        [Range(1, 5, ErrorMessage = "Please choose a rating between 1 and 5 stars.")]
        public int Rating { get; set; } = 5;

        [StringLength(1000, ErrorMessage = "Comment cannot exceed 1000 characters.")]
        public string? Comment { get; set; }
    }

    public async Task OnGetAsync(int? reservationId = null, int? orderId = null)
    {
        await LoadPageDataAsync(reservationId, orderId);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        if (!ModelState.IsValid)
        {
            await LoadPageDataAsync();
            return Page();
        }

        int? reservationId = null;
        int? orderId = null;

        if (Input.SelectedTarget.StartsWith("R:") && int.TryParse(Input.SelectedTarget[2..], out int rId))
        {
            reservationId = rId;
        }
        else if (Input.SelectedTarget.StartsWith("O:") && int.TryParse(Input.SelectedTarget[2..], out int oId))
        {
            orderId = oId;
        }

        var (success, message) = await _feedbackService.SubmitFeedbackAsync(
            customerId, reservationId, orderId, Input.Rating, Input.Comment);

        if (!success)
        {
            ModelState.AddModelError(string.Empty, message);
            await LoadPageDataAsync();
            return Page();
        }

        TempData["SuccessMessage"] = message;
        return RedirectToPage();
    }

    private async Task LoadPageDataAsync(int? preselectReservationId = null, int? preselectOrderId = null)
    {
        var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        var eligible = await _feedbackService.GetEligibleVisitsAsync(customerId);
        VisitOptions = eligible.Select(e =>
        {
            var val = e.ReservationId.HasValue ? $"R:{e.ReservationId}" : $"O:{e.OrderId}";
            bool isSelected = (preselectReservationId.HasValue && e.ReservationId == preselectReservationId) ||
                              (preselectOrderId.HasValue && e.OrderId == preselectOrderId);
            return new SelectListItem(e.DisplayLabel, val, isSelected);
        }).ToList();

        if (preselectReservationId.HasValue)
            Input.SelectedTarget = $"R:{preselectReservationId.Value}";
        else if (preselectOrderId.HasValue)
            Input.SelectedTarget = $"O:{preselectOrderId.Value}";

        PastFeedback = await _feedbackService.GetCustomerFeedbackAsync(customerId);
    }
}