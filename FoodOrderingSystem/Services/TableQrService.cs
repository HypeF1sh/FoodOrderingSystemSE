using System.Security.Cryptography;
using System.Text.Json;
using FoodOrderingSystem.ViewModels;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using QRCoder;

namespace FoodOrderingSystem.Services;

public class TableQrService : ITableQrService
{
    // A scan is trusted this long: enough for a whole meal, but a phone taken home stops working.
    // The session itself also ends after 60 idle minutes (Program.cs), whichever comes first.
    public const int ScanValidMinutes = 180;

    private const string SessionKey = "ScannedTable";

    // The purpose string ties the codes to this feature: a code made for anything else cannot be read here
    private const string ProtectorPurpose = "RROMS.TableQr.v1";

    private readonly IDataProtector _protector;
    private readonly IDiningAreaService _diningAreaService;

    // IDataProtectionProvider is built into ASP.NET Core (it also protects the login cookie), so it needs no registration
    public TableQrService(IDataProtectionProvider dataProtectionProvider, IDiningAreaService diningAreaService)
    {
        _protector = dataProtectionProvider.CreateProtector(ProtectorPurpose);
        _diningAreaService = diningAreaService;
    }

    public string CreateCode(int restaurantTableId) => _protector.Protect(restaurantTableId.ToString());

    public async Task<(bool Success, string Message)> ScanAsync(ISession session, string? code)
    {
        // 1. Only a code this server made can be read back; anything else (typed, edited, cut short) fails
        var tableId = ReadCode(code);
        if (tableId == null)
            return (false, "That QR code is not valid. Please ask a staff member for help.");

        // 2. The table must still be in use (Carynn's service: a removed table is deactivated, not deleted)
        var table = await _diningAreaService.GetTableAsync(tableId.Value);
        if (table == null || !table.IsActive)
            return (false, "That table is not taking orders. Please ask a staff member for help.");

        // 3. Remember it, with the time, so checkout can send orders to this table
        session.SetString(SessionKey, JsonSerializer.Serialize(new ScannedTable(table.RestaurantTableId, DateTime.Now)));
        return (true, $"You're at table {table.TableNumber}. Orders you place now go straight to this table.");
    }

    public int? GetScannedTableId(ISession session)
    {
        var json = session.GetString(SessionKey);
        if (string.IsNullOrEmpty(json)) return null;

        // A damaged value is treated as "not scanned" rather than crashing the page
        try
        {
            var scan = JsonSerializer.Deserialize<ScannedTable>(json);
            if (scan != null && DateTime.Now < scan.ScannedAt.AddMinutes(ScanValidMinutes))
                return scan.RestaurantTableId;
        }
        catch (JsonException)
        {
        }

        session.Remove(SessionKey);
        return null;
    }

    public async Task<List<TableQrCodeViewModel>> GetQrCodesAsync(Func<string, string> buildLink)
    {
        // GetTablesAsync includes the dining area and is already ordered by area, then table number
        var tables = await _diningAreaService.GetTablesAsync();

        return tables
            .Where(t => t.IsActive)
            .Select(t =>
            {
                var link = buildLink(CreateCode(t.RestaurantTableId));
                return new TableQrCodeViewModel
                {
                    RestaurantTableId = t.RestaurantTableId,
                    TableNumber = t.TableNumber,
                    DiningAreaName = t.DiningArea?.Name ?? string.Empty,
                    Link = link,
                    ImageDataUri = "data:image/png;base64," + Convert.ToBase64String(CreatePng(link))
                };
            })
            .ToList();
    }

    // Returns the table id inside a code, or null if the code was not made by this server
    private int? ReadCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;

        try
        {
            return int.TryParse(_protector.Unprotect(code), out var id) ? id : null;
        }
        catch (CryptographicException)
        {
            return null;
        }
    }

    // QRCoder draws the PNG itself (no System.Drawing), so it works the same on any machine.
    // ECC level Q still scans with about a quarter of the code scratched or covered.
    private static byte[] CreatePng(string text)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
        return new PngByteQRCode(data).GetGraphic(8);
    }

    // What is kept in the session after a scan
    private sealed record ScannedTable(int RestaurantTableId, DateTime ScannedAt);
}
