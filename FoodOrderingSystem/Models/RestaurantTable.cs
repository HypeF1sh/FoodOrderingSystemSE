// PB05 (Carynn) - one table: number, seats, area
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodOrderingSystem.Models;

public class RestaurantTable
{
    [Key]
    public int RestaurantTableId { get; set; }

    [Required]
    public int DiningAreaId { get; set; }

    [Required, StringLength(20)]
    public string TableNumber { get; set; } = string.Empty;

    [Range(1, 20)]
    public int Capacity { get; set; }

    // Inactive tables are hidden from booking but keep their history
    public bool IsActive { get; set; } = true;

    // Navigation properties
    [ForeignKey(nameof(DiningAreaId))]
    public DiningArea? DiningArea { get; set; }

    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}