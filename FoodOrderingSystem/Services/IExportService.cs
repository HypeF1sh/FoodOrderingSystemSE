// PB24 (Carynn) - report export (interface)
using FoodOrderingSystem.ViewModels;

namespace FoodOrderingSystem.Services;

// PB24: builds downloadable report files. The pages call this; they never build CSV themselves.
public interface IExportService
{
    /// <summary>
    /// Generic CSV builder: UTF-8 with BOM (so Excel opens it correctly), values with commas, quotes or
    /// line breaks are quoted, rows end with CRLF.
    /// </summary>
    byte[] BuildCsv(IEnumerable<string> headers, IEnumerable<IEnumerable<string>> rows);

    /// <summary>Customer activity report (PB23) as CSV, same numbers as the screen.</summary>
    byte[] BuildCustomerActivityCsv(IEnumerable<CustomerActivityRowViewModel> rows);

    /// <summary>Admin dashboard (PB22) as CSV: report settings, summary, sales, bookings and popular dishes.</summary>
    byte[] BuildDashboardCsv(DashboardViewModel dashboard, DateOnly from, DateOnly to);
}
