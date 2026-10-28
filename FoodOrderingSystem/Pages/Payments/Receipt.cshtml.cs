using System.Globalization;
using System.Security.Claims;
using FoodOrderingSystem.Data;
using FoodOrderingSystem.Services;
using FoodOrderingSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FoodOrderingSystem.Pages.Payments;

// Printable receipt for a successful payment (D4, folded into PB16).
// Customers see their own receipts; staff and admins see any (e.g. after recording cash in PB17).
[Authorize]
public class ReceiptModel : PageModel
{
    private readonly IPaymentService _paymentService;

    public ReceiptModel(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    public ReceiptViewModel? Receipt { get; set; }

    public bool IsStaffOrAdmin { get; set; }

    public async Task<IActionResult> OnGetAsync(int paymentId)
    {
        IsStaffOrAdmin = User.IsInRole(DbSeeder.StaffRole) || User.IsInRole(DbSeeder.AdminRole);
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        // Null for a missing or Failed payment, or someone else's receipt → 404, so ids cannot be guessed
        Receipt = await _paymentService.GetReceiptAsync(paymentId, userId, IsStaffOrAdmin);
        if (Receipt == null) return NotFound();

        // Cash tendered and change are not stored. The billing page passes them once, straight after
        // recording, as strings (TempData cannot hold decimals). A reload or reprint simply leaves them out.
        Receipt.AmountTendered = ReadDecimal("CashTendered");
        Receipt.Change = ReadDecimal("CashChange");

        return Page();
    }

    private decimal? ReadDecimal(string key) =>
        TempData[key] is string text && decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;
}
