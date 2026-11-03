using FoodOrderingSystem.ViewModels;

namespace FoodOrderingSystem.Services;

public class OccupancyService : IOccupancyService
{
    public const int MaxRangeDays = 366;
    public const int SlotMinutes = ReservationService.DiningDurationMinutes; // 90 minutes

    private static readonly string[] DayNames =
    {
        "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"
    };

    private readonly IReservationService _reservationService;
    private readonly IDiningAreaService _diningAreaService;
    private readonly IOperatingHoursService _operatingHoursService;

    public OccupancyService(
        IReservationService reservationService,
        IDiningAreaService diningAreaService,
        IOperatingHoursService operatingHoursService)
    {
        _reservationService = reservationService;
        _diningAreaService = diningAreaService;
        _operatingHoursService = operatingHoursService;
    }

    public OccupancyGrouping SuggestGrouping(DateOnly from, DateOnly to)
    {
        int days = to.DayNumber - from.DayNumber + 1;
        return days <= 31 ? OccupancyGrouping.Day : OccupancyGrouping.Week;
    }

    public async Task<(bool Success, string Message, OccupancyReportViewModel? Report)> GetOccupancyReportAsync(
        DateOnly from,
        DateOnly to,
        OccupancyGrouping? grouping = null)
    {
        if (to < from)
            return (false, "The 'To' date must be on or after the 'From' date.", null);

        int totalDays = to.DayNumber - from.DayNumber + 1;
        if (totalDays > MaxRangeDays)
            return (false, $"Please choose a date range of at most {MaxRangeDays} days.", null);

        var effectiveGrouping = grouping ?? SuggestGrouping(from, to);

        // 1. Read reservations via IReservationService and exclude Cancelled & NoShow
        var allReservations = await _reservationService.GetReservationsAsync(from, to, null);
        var validBookings = allReservations
            .Where(r => !string.Equals(r.Status, "Cancelled", StringComparison.OrdinalIgnoreCase)
                        && !string.Equals(r.Status, "NoShow", StringComparison.OrdinalIgnoreCase))
            .ToList();

        // 2. Read tables via IDiningAreaService and operating hours via IOperatingHoursService
        var tables = await _diningAreaService.GetTablesAsync();
        var weekHours = await _operatingHoursService.GetWeekAsync();

        // Calculate open minutes per DayOfWeek (0 = Sunday .. 6 = Saturday)
        var openMinutesByDayOfWeek = new Dictionary<int, double>();
        int earliestOpenHour = 23;
        int latestCloseHour = 0;

        for (int d = 0; d < 7; d++)
        {
            var dayConfig = weekHours.FirstOrDefault(w => (int)w.DayOfWeek == d);
            if (dayConfig == null || dayConfig.IsClosed || dayConfig.CloseTime <= dayConfig.OpenTime)
            {
                openMinutesByDayOfWeek[d] = 0;
            }
            else
            {
                double mins = (dayConfig.CloseTime - dayConfig.OpenTime).TotalMinutes;
                openMinutesByDayOfWeek[d] = Math.Max(0, mins);

                if (dayConfig.OpenTime.Hour < earliestOpenHour)
                    earliestOpenHour = dayConfig.OpenTime.Hour;

                int closeHr = dayConfig.CloseTime.Minute > 0
                    ? dayConfig.CloseTime.Hour
                    : Math.Max(dayConfig.OpenTime.Hour, dayConfig.CloseTime.Hour - 1);
                if (closeHr > latestCloseHour)
                    latestCloseHour = closeHr;
            }
        }

        if (earliestOpenHour > latestCloseHour)
        {
            earliestOpenHour = 10;
            latestCloseHour = 21;
        }

        // Expand range slightly if any historical booking falls outside current opening hours
        if (validBookings.Count > 0)
        {
            int minBookedHr = validBookings.Min(b => b.StartTime.Hour);
            int maxBookedHr = validBookings.Max(b => b.StartTime.Hour);
            earliestOpenHour = Math.Min(earliestOpenHour, minBookedHr);
            latestCloseHour = Math.Max(latestCloseHour, maxBookedHr);
        }

        // Sum total open minutes for a single table across all days in [from, to]
        double singleTableOpenMinutesDouble = 0;
        for (var cursor = from; cursor <= to; cursor = cursor.AddDays(1))
        {
            int dow = (int)cursor.DayOfWeek;
            singleTableOpenMinutesDouble += openMinutesByDayOfWeek.GetValueOrDefault(dow, 0);
        }
        int singleTableOpenMinutes = (int)Math.Round(singleTableOpenMinutesDouble);

        // ---- Build Peak Hours (Hourly Bar Series + 7xHour Heatmap) ----
        var hourlyPeaks = new List<HourlyPeakPointViewModel>();
        for (int hr = earliestOpenHour; hr <= latestCloseHour; hr++)
        {
            var slotBookings = validBookings.Where(r => r.StartTime.Hour == hr).ToList();
            var timeLabel = new TimeOnly(hr, 0).ToString("hh:mm tt");
            hourlyPeaks.Add(new HourlyPeakPointViewModel
            {
                Hour = hr,
                Label = timeLabel,
                BookingCount = slotBookings.Count,
                GuestCount = slotBookings.Sum(r => r.PartySize)
            });
        }

        var heatmapRows = new List<DayHourHeatmapRowViewModel>();
        int maxCell = 0;
        for (int d = 0; d < 7; d++)
        {
            var dayBookings = validBookings.Where(r => (int)r.ReservationDate.DayOfWeek == d).ToList();
            var hourDict = new Dictionary<int, int>();
            for (int hr = earliestOpenHour; hr <= latestCloseHour; hr++)
            {
                int count = dayBookings.Count(r => r.StartTime.Hour == hr);
                hourDict[hr] = count;
                if (count > maxCell)
                    maxCell = count;
            }

            heatmapRows.Add(new DayHourHeatmapRowViewModel
            {
                DayOfWeek = d,
                DayName = DayNames[d],
                BookingsByHour = hourDict,
                TotalBookings = dayBookings.Count
            });
        }

        // ---- Build Table Utilisation % ----
        var tableRows = new List<TableUtilizationRowViewModel>();
        foreach (var t in tables)
        {
            var tBookings = validBookings.Where(r => r.RestaurantTableId == t.RestaurantTableId).ToList();
            int bookedMins = tBookings.Count * SlotMinutes;
            int openMins = t.IsActive ? singleTableOpenMinutes : 0;

            double utilPercent = openMins > 0
                ? Math.Round(bookedMins * 100.0 / openMins, 1)
                : 0.0;

            tableRows.Add(new TableUtilizationRowViewModel
            {
                RestaurantTableId = t.RestaurantTableId,
                TableNumber = t.TableNumber,
                DiningAreaName = t.DiningArea?.Name ?? string.Empty,
                Capacity = t.Capacity,
                IsActive = t.IsActive,
                BookingCount = tBookings.Count,
                GuestCount = tBookings.Sum(r => r.PartySize),
                BookedMinutes = bookedMins,
                OpenMinutes = openMins,
                UtilizationPercent = utilPercent
            });
        }

        // Order tables by Utilisation % desc, then BookingCount desc, then TableNumber
        tableRows = tableRows
            .OrderByDescending(t => t.UtilizationPercent)
            .ThenByDescending(t => t.BookingCount)
            .ThenBy(t => t.TableNumber)
            .ToList();

        // ---- Build Trend Points (Day or Week) ----
        var trendPoints = new List<OccupancyTrendPointViewModel>();
        if (effectiveGrouping == OccupancyGrouping.Day)
        {
            for (var date = from; date <= to; date = date.AddDays(1))
            {
                var dayList = validBookings.Where(r => r.ReservationDate == date).ToList();
                trendPoints.Add(new OccupancyTrendPointViewModel
                {
                    Label = date.ToString("dd MMM"),
                    PeriodStart = date,
                    PeriodEnd = date,
                    BookingCount = dayList.Count,
                    GuestCount = dayList.Sum(r => r.PartySize)
                });
            }
        }
        else
        {
            var chunkStart = from;
            while (chunkStart <= to)
            {
                var chunkEnd = chunkStart.AddDays(6);
                if (chunkEnd > to)
                    chunkEnd = to;

                var weekList = validBookings
                    .Where(r => r.ReservationDate >= chunkStart && r.ReservationDate <= chunkEnd)
                    .ToList();

                string label = chunkStart == chunkEnd
                    ? chunkStart.ToString("dd MMM")
                    : $"{chunkStart:dd MMM} – {chunkEnd:dd MMM}";

                trendPoints.Add(new OccupancyTrendPointViewModel
                {
                    Label = label,
                    PeriodStart = chunkStart,
                    PeriodEnd = chunkEnd,
                    BookingCount = weekList.Count,
                    GuestCount = weekList.Sum(r => r.PartySize)
                });

                chunkStart = chunkEnd.AddDays(1);
            }
        }

        // ---- Headline Summary Metrics ----
        int totalBookings = validBookings.Count;
        int totalGuests = validBookings.Sum(r => r.PartySize);
        double avgParty = totalBookings > 0 ? Math.Round((double)totalGuests / totalBookings, 1) : 0.0;

        long totalBookedMinsAll = tableRows.Where(t => t.IsActive).Sum(t => (long)t.BookedMinutes);
        long totalOpenMinsAll = tableRows.Where(t => t.IsActive).Sum(t => (long)t.OpenMinutes);
        double overallUtil = totalOpenMinsAll > 0
            ? Math.Round(totalBookedMinsAll * 100.0 / totalOpenMinsAll, 1)
            : 0.0;

        var topHour = hourlyPeaks.OrderByDescending(h => h.BookingCount).FirstOrDefault();
        string peakHourLabel = topHour != null && topHour.BookingCount > 0
            ? $"{topHour.Label} ({topHour.BookingCount} {(topHour.BookingCount == 1 ? "booking" : "bookings")})"
            : "No bookings";

        var topDay = heatmapRows.OrderByDescending(d => d.TotalBookings).FirstOrDefault();
        string peakDayLabel = topDay != null && topDay.TotalBookings > 0
            ? $"{topDay.DayName} ({topDay.TotalBookings})"
            : "No bookings";

        var activeTables = tableRows.Where(t => t.IsActive).ToList();
        var busiest = activeTables.FirstOrDefault();
        var quietest = activeTables.LastOrDefault();

        string busiestLabel = busiest != null && busiest.BookingCount > 0
            ? $"Table {busiest.TableNumber} ({busiest.UtilizationPercent:0.0}%)"
            : "No bookings";

        string quietestLabel = quietest != null
            ? $"Table {quietest.TableNumber} ({quietest.UtilizationPercent:0.0}%)"
            : "N/A";

        var report = new OccupancyReportViewModel
        {
            From = from,
            To = to,
            TotalDays = totalDays,
            Grouping = effectiveGrouping,
            TotalBookings = totalBookings,
            TotalGuests = totalGuests,
            AveragePartySize = avgParty,
            OverallUtilizationPercent = overallUtil,
            PeakHourLabel = peakHourLabel,
            PeakDayLabel = peakDayLabel,
            BusiestTableLabel = busiestLabel,
            QuietestTableLabel = quietestLabel,
            EarliestHour = earliestOpenHour,
            LatestHour = latestCloseHour,
            HourlyPeaks = hourlyPeaks,
            HeatmapRows = heatmapRows,
            MaxHeatmapCellCount = maxCell,
            TableUtilizations = tableRows,
            TrendPoints = trendPoints
        };

        return (true, string.Empty, report);
    }
}