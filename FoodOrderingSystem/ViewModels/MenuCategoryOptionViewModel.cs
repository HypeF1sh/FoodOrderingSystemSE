// PB06/PB08 (Jordan) - view model: menu category option view model
namespace FoodOrderingSystem.ViewModels;

// A category id + name pair: used for the browse tabs and the admin category dropdown
public class MenuCategoryOptionViewModel
{
    public int MenuCategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
