// PB16 (Jordan) - one payment attempt for an order
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

    // Staff member who took a cash payment; null for online payments
    public string? RecordedByUserId { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal Amount { get; set; }

    [Required]
    public PaymentMethod Method { get; set; } = PaymentMethod.Cash;

    // One order can have several payment rows (a failed try, then a paid one); failed rows are never overwritten
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