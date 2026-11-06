// PB05 (Carynn) - opening hours for one day of the week
using System.ComponentModel.DataAnnotations;

namespace FoodOrderingSystem.Models;

public class OperatingHours
{
    [Key]
    public int OperatingHoursId { get; set; }

    [Range(0, 6)]
    public int DayOfWeek { get; set; }

    public TimeOnly OpenTime { get; set; }

    public TimeOnly CloseTime { get; set; }

    public bool IsClosed { get; set; } = false;
}