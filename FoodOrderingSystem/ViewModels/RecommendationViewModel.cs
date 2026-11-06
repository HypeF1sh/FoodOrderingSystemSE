// PB28 (Carynn) - view model: recommendation view model
namespace FoodOrderingSystem.ViewModels;

// PB28: one recommended dish, with just what the menu card needs
public class RecommendedDishViewModel
{
    public int MenuItemId { get; set; }
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
}

// PB28: the row shown at the top of /Menu. Empty (HasDishes = false) means "show nothing".
public class RecommendationViewModel
{
    public string Title { get; set; } = string.Empty;
    public List<RecommendedDishViewModel> Dishes { get; set; } = new();

    public bool HasDishes => Dishes.Count > 0;
}
