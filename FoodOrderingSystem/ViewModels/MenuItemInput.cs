// PB06/PB08 (Jordan) - view model: menu item input
using System.ComponentModel.DataAnnotations;

namespace FoodOrderingSystem.ViewModels;

// Form model for Create/Edit Item. The page binds this, never the MenuItem entity,
// so a crafted POST cannot set ImageUrl or other fields directly (over-posting)
public class MenuItemInput
{
    public int MenuItemId { get; set; }

    [Display(Name = "Category")]
    [Range(1, int.MaxValue, ErrorMessage = "Please choose a category.")]
    public int MenuCategoryId { get; set; }

    [Required(ErrorMessage = "Please enter the dish name.")]
    [StringLength(150, ErrorMessage = "Name must be 150 characters or fewer.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Description must be 500 characters or fewer.")]
    public string? Description { get; set; }

    // Range limits are parsed with the invariant culture so "0.01" means the same on every PC
    [Range(typeof(decimal), "0.01", "9999.99", ParseLimitsInInvariantCulture = true,
        ErrorMessage = "Price must be between RM 0.01 and RM 9,999.99.")]
    public decimal Price { get; set; }

    [Display(Name = "Available to order")]
    public bool IsAvailable { get; set; } = true;

    // Optional on both create and edit: leaving it empty keeps the current image (or the placeholder)
    [Display(Name = "Photo")]
    public IFormFile? ImageFile { get; set; }
}
