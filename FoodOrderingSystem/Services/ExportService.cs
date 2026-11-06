// PB24 (Carynn) - report export to CSV (Excel-safe)
using System.Globalization;
using System.Text;
using FoodOrderingSystem.ViewModels;

namespace FoodOrderingSystem.Services;

public class ExportService : IExportService
{
    // UTF-8 with a byte order mark, so Excel reads accents and non-English names correctly
    private static readonly UTF8Encoding Utf8Bom = new(encoderShouldEmitUTF8Identifier: true);

    public byte[] BuildCsv(IEnumerable<string> headers, IEnumerable<IEnumerable<string>> rows)
    {
        var sb = new StringBuilder();

        sb.Append(string.Join(",", headers.Select(Escape))).Append("\r\n");
        foreach (var row in rows)
            sb.Append(string.Join(",", row.Select(Escape))).Append("\r\n");

        var preamble = Utf8Bom.GetPreamble();          // the BOM bytes
        var body = Utf8Bom.GetBytes(sb.ToString());    // GetBytes does not add the BOM itself
        return preamble.Concat(body).ToArray();
    }

    public byte[] BuildCustomerActivityCsv(IEnumerable<CustomerActivityRowViewModel> rows)
    {
        var headers = new[] { "Customer", "Email", "Reservations", "Orders", "Total spent (RM)", "Last visit" };

        var data = rows.Select(r => (IEnumerable<string>)new[]
        {
            SafeText(r.CustomerName),
            SafeText(r.CustomerEmail),
            r.ReservationCount.ToString(CultureInfo.InvariantCulture),
            r.OrderCount.ToString(CultureInfo.InvariantCulture),
            Money(r.TotalSpent),
            r.LastVisit?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty
        });

        return BuildCsv(headers, data);
    }

    public byte[] BuildDashboardCsv(DashboardViewModel dashboard, DateOnly from, DateOnly to)
    {
        // One flat table (Section, Label, Value) so it opens cleanly in Excel and can be filtered there
        var rows = new List<IEnumerable<string>>();

        void Add(string section, string label, string value) =>
            rows.Add(new[] { section, SafeText(label), value });

        Add("Report", "From", from.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        Add("Report", "To", to.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        Add("Report", "Grouped by", dashboard.Grouping.ToString());

        // Same numbers as the stat tiles on the screen
        Add("Summary", "Revenue (RM)", Money(Convert.ToDecimal(dashboard.Revenue)));
        Add("Summary", "Average order (RM)", Money(Convert.ToDecimal(dashboard.AverageOrderValue)));
        Add("Summary", "Paid orders", Plain(dashboard.PaidOrderCount));
        Add("Summary", "Orders", Plain(dashboard.OrderCount));
        Add("Summary", "Bookings", Plain(dashboard.BookingCount));
        Add("Summary", "Guests", Plain(dashboard.CoverCount));

        foreach (var point in dashboard.SalesByPeriod)
            Add("Sales (RM)", point.Label, Money(Convert.ToDecimal(point.Value)));

        foreach (var point in dashboard.BookingsByPeriod)
            Add("Bookings", point.Label, Plain(point.Value));

        foreach (var item in dashboard.PopularItems)
            Add("Popular dishes (quantity sold)", item.Name, Plain(item.QuantitySold));

        return BuildCsv(new[] { "Section", "Label", "Value" }, rows);
    }

    // ---------- helpers ----------

    /// <summary>Money with 2 decimals and no thousands separator, so Excel treats it as a number.</summary>
    public static string Money(decimal value) => value.ToString("F2", CultureInfo.InvariantCulture);

    /// <summary>A whole number or other value as plain invariant text.</summary>
    private static string Plain(object? value) => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;

    /// <summary>
    /// Text typed by users (names, emails) could start with = + - @ and be run as a formula by Excel
    /// (CSV injection). A leading apostrophe makes Excel treat it as plain text.
    /// </summary>
    public static string SafeText(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return "=+-@\t\r".IndexOf(value[0]) >= 0 ? "'" + value : value;
    }

    // Quote a value if it contains a comma, a quote or a line break; double any quotes inside it
    private static string Escape(string? value)
    {
        value ??= string.Empty;
        if (value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0)
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        return value;
    }
}
