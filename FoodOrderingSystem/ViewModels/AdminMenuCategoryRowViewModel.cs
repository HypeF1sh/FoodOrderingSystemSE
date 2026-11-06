// PB06/PB08 (Jordan) - view model: admin menu category row view model
namespace FoodOrderingSystem.ViewModels;

// One row of the Categories table on /Admin/Menu, also used by the Delete Category page
public class AdminMenuCategoryRowViewModel
{
    public int MenuCategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public int ItemCount { get; set; }
}
