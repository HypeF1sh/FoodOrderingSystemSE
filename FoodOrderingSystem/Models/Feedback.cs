// PB19 (Wallace) - rating and comment after a completed booking
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodOrderingSystem.Models;

public class Feedback
{
    [Key]
    public int FeedbackId { get; set; }

    [Required]
    public string CustomerId { get; set; } = string.Empty;

    // A review is for a booking OR an order, so exactly one of these is set
    public int? ReservationId { get; set; }

    public int? OrderId { get; set; }

    [Range(1, 5)]
    public int Rating { get; set; }

    [StringLength(1000)]
    public string Comment { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(CustomerId))]
    public ApplicationUser? Customer { get; set; }

    [ForeignKey(nameof(ReservationId))]
    public Reservation? Reservation { get; set; }

    [ForeignKey(nameof(OrderId))]
    public Order? Order { get; set; }
}