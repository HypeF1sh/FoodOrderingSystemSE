// PB14 (Jordan) - table QR ordering (interface)
using FoodOrderingSystem.ViewModels;
using Microsoft.AspNetCore.Http;

namespace FoodOrderingSystem.Services;

// Table QR ordering (PB14): the code printed on each table, and remembering which table a phone scanned.
// Like ICartService, the page passes in HttpContext.Session, so the service has no hidden dependency on the request.
public interface ITableQrService
{
    /// <summary>The tamper-proof code that goes in a table's QR link. Typing another table's id does not work,
    /// because only this server can make a valid code.</summary>
    string CreateCode(int restaurantTableId);

    /// <summary>Checks a scanned code and, if it is for an active table, remembers that table in the session
    /// for ScanValidMinutes. Returns (success, message).</summary>
    Task<(bool Success, string Message)> ScanAsync(ISession session, string? code);

    /// <summary>The table this phone scanned, or null if it never scanned one or the scan is too old.</summary>
    int? GetScannedTableId(ISession session);

    /// <summary>Every active table with its QR image, for the printable QR page.
    /// buildLink turns a code into the full link (the page knows the site's address, the service does not).</summary>
    Task<List<TableQrCodeViewModel>> GetQrCodesAsync(Func<string, string> buildLink);
}
