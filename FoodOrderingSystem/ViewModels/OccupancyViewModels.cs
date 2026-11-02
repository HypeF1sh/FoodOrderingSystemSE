namespace FoodOrderingSystem.ViewModels;

public enum OccupancyGrouping
{
    Day,
    Week
}

public class OccupancyReportViewModel
{
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
    public int TotalDays { get; set; }
    public OccupancyGrouping Grouping { get; set; }

    // Headline stats
    public int TotalBookings { get; set; }
    public int TotalGuests { get; set; }
    public double AveragePartySize { get; set; }
    public double OverallUtilizationPercent { get; set; }
    public string PeakHourLabel { get; set; } = "N/A";
    public string PeakDayLabel { get; set; } = "N/A";
    public string BusiestTableLabel { get; set; } = "N/A";
    public string QuietestTableLabel { get; set; } = "N/A";

    // 1. Peak hours (Chart + 7xHour Heatmap table)
    public int EarliestHour { get; set; } = 9;
    public int LatestHour { get; set; } = 22;
    public List<HourlyPeakPointViewModel> HourlyPeaks { get; set; } = new();
    public List<DayHourHeatmapRowViewModel> HeatmapRows { get; set; } = new();
    public int MaxHeatmapCellCount { get; set; }

    // 2. Table utilisation (Chart + Table)
    public List<TableUtilizationRowViewModel> TableUtilizations { get; set; } = new();

    // 3. Trend over time (Chart + Table)
    public List<OccupancyTrendPointViewModel> TrendPoints { get; set; } = new();
}

public class HourlyPeakPointViewModel
{
    public int Hour { get; set; }
    public string Label { get; set; } = string.Empty;
    public int BookingCount { get; set; }
    public int GuestCount { get; set; }
}

public class DayHourHeatmapRowViewModel
{
    public int DayOfWeek { get; set; }
    public string DayName { get; set; } = string.Empty;
    public Dictionary<int, int> BookingsByHour { get; set; } = new();
    public int TotalBookings { get; set; }
}

public class TableUtilizationRowViewModel
{
    public int RestaurantTableId { get; set; }
    public string TableNumber { get; set; } = string.Empty;
    public string DiningAreaName { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public bool IsActive { get; set; }
    public int BookingCount { get; set; }
    public int GuestCount { get; set; }
    public int BookedMinutes { get; set; }
    public int OpenMinutes { get; set; }
    public double UtilizationPercent { get; set; }
}

public class OccupancyTrendPointViewModel
{
    public string Label { get; set; } = string.Empty;
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public int BookingCount { get; set; }
    public int GuestCount { get; set; }
}