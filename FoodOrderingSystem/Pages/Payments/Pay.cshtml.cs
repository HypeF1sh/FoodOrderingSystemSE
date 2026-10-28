using System.Security.Claims;
using FoodOrderingSystem.Data;
using FoodOrderingSystem.Models.Enums;
using FoodOrderingSystem.Services;
using FoodOrderingSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FoodOrderingSystem.Pages.Payments;

// Customer pays for their own order online, by card or e-wallet (PB16)
[Authorize(Roles = DbSeeder.CustomerRole)]
public class PayModel : PageModel
{
    private readonly IPaymentService _paymentService;

    public PayModel(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [BindProperty]
    public PaymentInput Input { get; set; } = new();

    public PaymentPageViewModel? Order { get; set; }

    public async Task<IActionResult> OnGetAsync(int orderId)
    {
        // The service only returns the customer's own order, so another order id gives 404
        Order = await _paymentService.GetPaymentPageAsync(orderId, CustomerId());
        if (Order == null) return NotFound();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int orderId)
    {
        Order = await _paymentService.GetPaymentPageAsync(orderId, CustomerId());
        if (Order == null) return NotFound();

        // 1. Every field is [Required], so drop the errors for the method the customer did not choose
        var unusedFields = Input.Method == PaymentMethod.Card ? PaymentInput.WalletFields : PaymentInput.CardFields;
        foreach (var field in unusedFields)
            ModelState.Remove($"{nameof(Input)}.{field}");

        // 2. Business checks the annotations cannot do: Luhn check and expiry date
        if (ModelState.IsValid)
        {
            foreach (var (field, error) in _paymentService.CheckPaymentDetails(Input))
                ModelState.AddModelError($"{nameof(Input)}.{field}", error);
        }

        if (!ModelState.IsValid) return PageWithoutCardSecrets();

        // 3. The service re-checks the order is still unpaid, charges the gateway and saves the attempt
        var (success, message, paymentId) = await _paymentService.PayOnlineAsync(orderId, CustomerId(), Input);
        if (!success || paymentId == null)
        {
            ModelState.AddModelError(string.Empty, message);
            // Reload so the new Failed attempt shows in the list
            Order = await _paymentService.GetPaymentPageAsync(orderId, CustomerId());
            if (Order == null) return NotFound();
            return PageWithoutCardSecrets();
        }

        TempData["SuccessMessage"] = message;
        return RedirectToPage("./Receipt", new { paymentId });
    }

    private string CustomerId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    // Security rule: the card number and CVV are never sent back in the HTML.
    // SetModelValue blanks what the input boxes show but keeps any error message attached to them.
    private PageResult PageWithoutCardSecrets()
    {
        Input.CardNumber = null;
        Input.Cvv = null;
        ModelState.SetModelValue($"{nameof(Input)}.{nameof(PaymentInput.CardNumber)}", null, string.Empty);
        ModelState.SetModelValue($"{nameof(Input)}.{nameof(PaymentInput.Cvv)}", null, string.Empty);
        return Page();
    }
}
