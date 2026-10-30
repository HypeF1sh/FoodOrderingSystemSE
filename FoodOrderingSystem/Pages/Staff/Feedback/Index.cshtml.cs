using FoodOrderingSystem.Services;
using FoodOrderingSystem.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FoodOrderingSystem.Pages.Staff.Feedback;

public class IndexModel : PageModel
{
    private readonly IFeedbackService _feedbackService;

    public IndexModel(IFeedbackService feedbackService)
    {
        _feedbackService = feedbackService;
    }

    [BindProperty(SupportsGet = true)]
    public int? RatingFilter { get; set; }

    public List<FeedbackItemViewModel> Feedbacks { get; set; } = new();

    public async Task OnGetAsync()
    {
        Feedbacks = await _feedbackService.GetAllFeedbackAsync(RatingFilter);
    }
}