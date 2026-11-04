using FoodOrderingSystem.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FoodOrderingSystem.Pages.Orders;

// Where the QR code on a table points (PB14 table QR ordering): /Orders/Table?code=...
// Open to everyone, so a customer can scan before logging in; the table is kept in the session and is still
// there after login. Placing the order itself still needs a customer account (Checkout is [Authorize]).
public class TableModel : PageModel
{
    private readonly ITableQrService _tableQrService;

    public TableModel(ITableQrService tableQrService)
    {
        _tableQrService = tableQrService;
    }

    [BindProperty(SupportsGet = true)]
    public string? Code { get; set; }

    // A GET that changes the session is acceptable here: it only remembers a table, it does not place an order,
    // and a phone camera can only open a link (it cannot send a form)
    public async Task<IActionResult> OnGetAsync()
    {
        var (success, message) = await _tableQrService.ScanAsync(HttpContext.Session, Code);

        TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
        return RedirectToPage("/Menu/Index");
    }
}
