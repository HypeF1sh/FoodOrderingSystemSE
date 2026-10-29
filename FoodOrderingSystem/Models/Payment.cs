using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FoodOrderingSystem.Models.Enums;

namespace FoodOrderingSystem.Models;

public class Payment
{
    [Key]
    public int PaymentId { get; set; }

    [Required]
    public int OrderId { get; set; }

    public string? RecordedByUserId { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal Amount { get; set; }

    [Required]
    public PaymentMethod Method { get; set; } = PaymentMethod.Cash;

    [Required]
    public PaymentStatus Status { get; set; } = PaymentStatus.Unpaid;

    [StringLength(100)]
    public string TransactionRef { get; set; } = string.Empty;

    public DateTime PaidAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(OrderId))]
    public Order? Order { get; set; }

    [ForeignKey(nameof(RecordedByUserId))]
    public ApplicationUser? RecordedByUser { get; set; }
}