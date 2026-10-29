using System.ComponentModel.DataAnnotations;

namespace FoodOrderingSystem.ViewModels;

// The only thing the customer types at checkout. Items and prices come from the cart, never the form.
public class CheckoutInput
{
    [StringLength(500, ErrorMessage = "Notes must be 500 characters or fewer.")]
    [Display(Name = "Notes for the kitchen (optional)")]
    public string? Notes { get; set; }
}
