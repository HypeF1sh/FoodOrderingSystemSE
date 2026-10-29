using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FoodOrderingSystem.Models.Enums;

namespace FoodOrderingSystem.Models;

public class Reservation
{
    [Key]
    public int ReservationId { get; set; }

    [Required]
    public string CustomerId { get; set; } = string.Empty;

    [Required]
    public int RestaurantTableId { get; set; }

    [Required]
    public DateOnly ReservationDate { get; set; }

    [Required]
    public TimeOnly StartTime { get; set; }

    [Required]
    public TimeOnly EndTime { get; set; }

    [Range(1, 20)]
    public int PartySize { get; set; }

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

    public ICollection<Order> Orders { get; set; } = new List<Order>();
}