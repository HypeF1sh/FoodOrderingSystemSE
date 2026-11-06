// PB14 (Jordan) - one dish line on an order, price saved at order time
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodOrderingSystem.Models;

public class OrderItem
{
    [Key]
    public int OrderItemId { get; set; }

    [Required]
    public int OrderId { get; set; }

    [Required]
    public int MenuItemId { get; set; }

    [Range(1, 100)]
    public int Quantity { get; set; }

    // Price at the time of ordering: a later menu price change never changes old orders or receipts
    [Column(TypeName = "decimal(10,2)")]
    public decimal UnitPrice { get; set; }

    // Navigation properties
    [ForeignKey(nameof(OrderId))]
    public Order? Order { get; set; }

    [ForeignKey(nameof(MenuItemId))]
    public MenuItem? MenuItem { get; set; }
}