using System.ComponentModel.DataAnnotations.Schema;

namespace FoodOrderingSystem.ViewModels;

// The admin "Dish sales" report (PB22): every dish on the menu for one date range. Built by ReportService, read-only.
public class DishSalesViewModel
{
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }

    // Best seller first; dishes that sold nothing are at the bottom with 0
    public List<PopularItemViewModel> Dishes { get; set; } = new();

    public int TotalQuantity { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal TotalSales { get; set; }
}
