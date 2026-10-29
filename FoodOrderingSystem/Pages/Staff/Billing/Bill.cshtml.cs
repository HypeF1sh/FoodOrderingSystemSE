using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Security.Claims;
using FoodOrderingSystem.Services;
using FoodOrderingSystem.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FoodOrderingSystem.Pages.Staff.Billing;

public class BillModel : PageModel
{
    private readonly IPaymentService _paymentService;

    public BillModel(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    public BillViewModel? Bill { get; set; }

    [BindProperty]
    [Required(ErrorMessage = "Please enter the cash amount tendered.")]
    [Range(0.01, 100000, ErrorMessage = "Enter a valid tendered amount.")]
    public decimal AmountTendered { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Bill = await _paymentService.GetBillAsync(id);
        if (Bill == null)
            return NotFound();

        AmountTendered = Bill.TotalAmount;
        return Page();
    }

    public async Task<IActionResult> OnPostRecordCashAsync(int id)
    {
        var staffUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        var (success, message, paymentId, change) = await _paymentService.RecordCashPaymentAsync(
            id, AmountTendered, staffUserId);

        if (!success || !paymentId.HasValue)
        {
            TempData["ErrorMessage"] = message;
            Bill = await _paymentService.GetBillAsync(id);
            return Bill == null ? NotFound() : Page();
        }

        TempData["SuccessMessage"] = message;
        // Pass tendered and change amounts in invariant culture for the Receipt page (per IPaymentService contract)
        TempData["CashTendered"] = AmountTendered.ToString("0.00", CultureInfo.InvariantCulture);
        TempData["CashChange"] = change.ToString("0.00", CultureInfo.InvariantCulture);

        return RedirectToPage("/Payments/Receipt", new { id = paymentId.Value });
    }

    public async Task<IActionResult> OnPostRefundAsync(int id, int paymentId)
    {
        var staffUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var (success, message) = await _paymentService.RefundPaymentAsync(paymentId, staffUserId);

        if (success)
            TempData["SuccessMessage"] = message;
        else
            TempData["ErrorMessage"] = message;

        return RedirectToPage(new { id });
    }
}