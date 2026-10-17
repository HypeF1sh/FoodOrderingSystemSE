namespace FoodOrderingSystem.ViewModels;

public class DiningAreaOptionViewModel
{
    public int DiningAreaId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class AvailableTableViewModel
{
    public int RestaurantTableId { get; set; }
    public string TableNumber { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public int DiningAreaId { get; set; }
    public string DiningAreaName { get; set; } = string.Empty;
}