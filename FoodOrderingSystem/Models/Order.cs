// PB14 (Jordan) - customer order: status, total, booking or scanned table
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FoodOrderingSystem.Models.Enums;

namespace FoodOrderingSystem.Models;

public class Order
{
    [Key]
    public int OrderId { get; set; }

    [Required]
    public string CustomerId { get; set; } = string.Empty;

    public int? ReservationId { get; set; }

    // The table the order is for: from the customer's booking, or from the table QR a walk-in scanned
    // (then ReservationId is null). Null only for old orders placed before table QR existed.
    public int? RestaurantTableId { get; set; }

    [Required]
    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    [Column(TypeName = "decimal(10,2)")]
    public decimal TotalAmount { get; set; }

    [StringLength(500)]
    public string Notes { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(CustomerId))]
    public ApplicationUser? Customer { get; set; }

    [ForeignKey(nameof(ReservationId))]
    public Reservation? Reservation { get; set; }

    [ForeignKey(nameof(RestaurantTableId))]
    public RestaurantTable? RestaurantTable { get; set; }

    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}