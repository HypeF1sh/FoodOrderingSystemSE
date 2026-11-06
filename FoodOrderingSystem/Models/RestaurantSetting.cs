// PB07 (Carynn) - key-value restaurant setting (e.g. Address, Phone)
using System.ComponentModel.DataAnnotations;

namespace FoodOrderingSystem.Models;

public class RestaurantSetting
{
    [Key]
    public int RestaurantSettingId { get; set; }

    [Required, StringLength(100)]
    public string Key { get; set; } = string.Empty;

    [Required, StringLength(500)]
    public string Value { get; set; } = string.Empty;
}