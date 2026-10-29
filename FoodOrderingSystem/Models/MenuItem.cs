using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodOrderingSystem.Models;

public class MenuItem
{
    [Key]
    public int MenuItemId { get; set; }

    [Required]
    public int MenuCategoryId { get; set; }

    [Required, StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    [Column(TypeName = "decimal(10,2)")]
    public decimal Price { get; set; }

    [StringLength(500)]
    public string ImageUrl { get; set; } = string.Empty;

    public bool IsAvailable { get; set; } = true;

    // Navigation properties
    [ForeignKey(nameof(MenuCategoryId))]
    public MenuCategory? MenuCategory { get; set; }

    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}