using FoodOrderingSystem.Services;
using FoodOrderingSystem.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FoodOrderingSystem.Pages.Staff.Billing;

public class IndexModel : PageModel
{
    private readonly IPaymentService _paymentService;

    public IndexModel(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [BindProperty(SupportsGet = true)]
    public DateOnly? FilterDate { get; set; }

    public DateOnly EffectiveDate { get; set; }
    public List<UnpaidOrderRowViewModel> UnpaidOrders { get; set; } = new();

    public async Task OnGetAsync()
    {
        EffectiveDate = FilterDate ?? DateOnly.FromDateTime(DateTime.Today);
        UnpaidOrders = await _paymentService.GetUnpaidOrdersAsync(EffectiveDate);
    }

    public async Task<IActionResult> OnPostLookupOrderAsync(int? orderId)
    {
        if (!orderId.HasValue || orderId.Value <= 0)
        {
            TempData["ErrorMessage"] = "Please enter a valid Order ID.";
            return RedirectToPage();
        }

        var bill = await _paymentService.GetBillAsync(orderId.Value);
        if (bill == null)
        {
            TempData["ErrorMessage"] = $"Order #{orderId.Value} was not found.";
            return RedirectToPage();
        }

        return RedirectToPage("Bill", new { id = orderId.Value });
    }
}