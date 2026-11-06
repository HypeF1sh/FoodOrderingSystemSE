// PB06/PB08 (Jordan) - view model: menu category section view model
namespace FoodOrderingSystem.ViewModels;

// One category heading on /Menu with the dishes listed under it
public class MenuCategorySectionViewModel
{
    public int MenuCategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<MenuItemCardViewModel> Items { get; set; } = new();
}
