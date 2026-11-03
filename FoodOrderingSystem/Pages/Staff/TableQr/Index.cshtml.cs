using FoodOrderingSystem.Data;
using FoodOrderingSystem.Services;
using FoodOrderingSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FoodOrderingSystem.Pages.Staff.TableQr;

// Printable sheet with one QR code per active table (PB14 table QR ordering). Staff print it and stick
// each code on its table; scanning it opens the menu with that table attached.
[Authorize(Roles = $"{DbSeeder.StaffRole},{DbSeeder.AdminRole}")]
public class IndexModel : PageModel
{
    private readonly ITableQrService _tableQrService;

    public IndexModel(ITableQrService tableQrService)
    {
        _tableQrService = tableQrService;
    }

    public List<TableQrCodeViewModel> Codes { get; set; } = new();

    public async Task OnGetAsync()
    {
        // The link uses the address this page was opened on, so open it on the address phones can reach
        // (the PC's network address, not localhost) before printing
        Codes = await _tableQrService.GetQrCodesAsync(code =>
            Url.Page("/Orders/Table", null, new { code }, Request.Scheme) ?? string.Empty);
    }
}
