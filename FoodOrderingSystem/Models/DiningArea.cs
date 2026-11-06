// PB05 (Carynn) - dining area (e.g. Garden) that holds tables
using System.ComponentModel.DataAnnotations;

namespace FoodOrderingSystem.Models;

public class DiningArea
{
    [Key]
    public int DiningAreaId { get; set; }

    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    // Navigation properties
    public ICollection<RestaurantTable> RestaurantTables { get; set; } = new List<RestaurantTable>();
}