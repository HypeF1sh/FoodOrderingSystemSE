using FoodOrderingSystem.ViewModels;

namespace FoodOrderingSystem.Services;

// PB28: "Recommended for you" on the menu page. Read-only.
public interface IRecommendationService
{
    /// <summary>
    /// 2 to 4 dishes for a logged-in customer, based on what they ordered before (cancelled orders do not
    /// count), topped up with the restaurant's most popular dishes in the same categories. A customer with no
    /// orders gets the restaurant's top dishes from the last 30 days ("Popular right now"). Only available
    /// dishes in active categories. Fewer than 2 dishes means an empty result (the page hides the row).
    /// fullMenu must be the whole menu (no category or search filter), as returned by IMenuService.
    /// </summary>
    Task<RecommendationViewModel> GetRecommendationsAsync(string customerId, MenuBrowseViewModel fullMenu);
}
