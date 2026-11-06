// PB05 (Carynn) - view model: operating hours day view model
namespace FoodOrderingSystem.ViewModels;   // <-- match your project namespace

public class OperatingHoursDayViewModel
{
    private static readonly string[] Names =
        { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };

    public int DayOfWeek { get; set; }            // 0 = Sunday ... 6 = Saturday
    public TimeOnly OpenTime { get; set; }
    public TimeOnly CloseTime { get; set; }
    public bool IsClosed { get; set; }

    public string DayName => DayOfWeek is >= 0 and <= 6 ? Names[DayOfWeek] : "Unknown";
}
