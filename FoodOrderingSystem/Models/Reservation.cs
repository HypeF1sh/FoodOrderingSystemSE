// PB10 (Wallace) - table booking: date, time slot, party size, status
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FoodOrderingSystem.Models.Enums;

namespace FoodOrderingSystem.Models;

public class Reservation
{
    [Key]
    public int ReservationId { get; set; }

    // Identity user ids are strings
    [Required]
    public string CustomerId { get; set; } = string.Empty;

    [Required]
    public int RestaurantTableId { get; set; }

    [Required]
    public DateOnly ReservationDate { get; set; }

    [Required]
    public TimeOnly StartTime { get; set; }

    // Always StartTime + 90 min (set by ReservationService)
    [Required]
    public TimeOnly EndTime { get; set; }

    [Range(1, 20)]
    public int PartySize { get; set; }

    // New bookings start Pending until staff confirm
    [Required]
    public ReservationStatus Status { get; set; } = ReservationStatus.Pending;

    [StringLength(500)]
    public string SpecialRequests { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(CustomerId))]
    public ApplicationUser? Customer { get; set; }

    [ForeignKey(nameof(RestaurantTableId))]
    public RestaurantTable? RestaurantTable { get; set; }

    // Orders placed for this booking (pre-orders and orders at the table)
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}