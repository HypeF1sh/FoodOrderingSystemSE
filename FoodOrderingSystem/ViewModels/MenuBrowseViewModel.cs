// PB06/PB08 (Jordan) - view model: menu browse view model
namespace FoodOrderingSystem.ViewModels;

// Everything the public /Menu page needs in one object
public class MenuBrowseViewModel
{
    public List<MenuCategoryOptionViewModel> Tabs { get; set; } = new();
    public List<MenuCategorySectionViewModel> Sections { get; set; } = new();

    // The filter that was actually applied (an unknown category id falls back to null = "All")
    public int? SelectedCategoryId { get; set; }
    public string? Search { get; set; }
}
