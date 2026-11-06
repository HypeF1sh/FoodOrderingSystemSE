// PB06/PB08 (Jordan) - view model: menu category input
using System.ComponentModel.DataAnnotations;

namespace FoodOrderingSystem.ViewModels;

// Form model for Create/Edit Category. The page binds this, never the MenuCategory entity,
// so a crafted POST cannot set fields the form does not show (over-posting)
public class MenuCategoryInput
{
    public int MenuCategoryId { get; set; }

    [Required(ErrorMessage = "Please enter a category name.")]
    [StringLength(100, ErrorMessage = "Name must be 100 characters or fewer.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Description must be 500 characters or fewer.")]
    public string? Description { get; set; }

    [Display(Name = "Display order")]
    [Range(0, 999, ErrorMessage = "Display order must be between 0 and 999.")]
    public int DisplayOrder { get; set; }

    [Display(Name = "Show this category on the menu")]
    public bool IsActive { get; set; } = true;
}
