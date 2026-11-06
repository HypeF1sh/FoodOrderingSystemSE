// PB22 (Jordan) - view model: popular item view model
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodOrderingSystem.ViewModels;

// One dish in the dashboard's "Popular dishes" chart and the full "Dish sales" report (PB22)
public class PopularItemViewModel
{
    public int MenuItemId { get; set; }
    public string Name { get; set; } = string.Empty;

    // Only filled for the Dish sales report
    public string CategoryName { get; set; } = string.Empty;
    public bool IsAvailable { get; set; } = true;

    public int QuantitySold { get; set; }

    // Quantity × unit price at the time of the order
    [Column(TypeName = "decimal(10,2)")]
    public decimal ItemSales { get; set; }
}
