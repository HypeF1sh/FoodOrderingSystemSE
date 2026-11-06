// PB28 (Carynn) - dish recommendations from order history
using FoodOrderingSystem.ViewModels;

namespace FoodOrderingSystem.Services;

public class RecommendationService : IRecommendationService
{
    private const int MinShown = 1;
    private const int MaxShown = 3;
    private const int PopularDays = 30;
    private const int Candidates = 30;   // ids asked for: some dishes may be sold out or hidden by now

    private readonly IOrderService _orderService;

    // Orders are read through IOrderService; dishes come from the menu the page already loaded
    public RecommendationService(IOrderService orderService)
    {
        _orderService = orderService;
    }

    public async Task<RecommendationViewModel> GetRecommendationsAsync(string customerId, MenuBrowseViewModel fullMenu)
    {
        // Only available dishes in active categories (the browse menu holds active categories only)
        var available = fullMenu.Sections
            .SelectMany(section => section.Items
                .Where(item => item.IsAvailable)
                .Select(item => new RecommendedDishViewModel
                {
                    MenuItemId = item.MenuItemId,
                    CategoryId = section.MenuCategoryId,
                    Name = item.Name,
                    Description = item.Description,
                    Price = item.Price,
                    ImageUrl = item.ImageUrl
                }))
            .GroupBy(d => d.MenuItemId)
            .Select(g => g.First())
            .ToDictionary(d => d.MenuItemId);

        var chosen = new List<RecommendedDishViewModel>();

        // 1. The customer's own most ordered dishes
        var favouriteIds = await _orderService.GetTopItemIdsAsync(customerId, null, Candidates);
        AddInOrder(chosen, favouriteIds, available);

        // New customer (or every favourite is unavailable): what the restaurant sold most in the last 30 days
        if (chosen.Count == 0)
        {
            var since = DateOnly.FromDateTime(DateTime.Today.AddDays(-PopularDays));
            var recentIds = await _orderService.GetTopItemIdsAsync(null, since, Candidates);
            AddInOrder(chosen, recentIds, available);
            return Build("Popular right now", chosen);
        }

        // 2. Top up with the restaurant's most popular dishes in the same categories
        if (chosen.Count < MaxShown)
        {
            var categoryIds = chosen.Select(d => d.CategoryId).ToHashSet();
            var popularIds = await _orderService.GetTopItemIdsAsync(null, null, Candidates);

            AddInOrder(
                chosen,
                popularIds.Where(id => available.TryGetValue(id, out var dish) && categoryIds.Contains(dish.CategoryId)),
                available);
        }

        return Build("Recommended for you", chosen);
    }

    // Adds dishes in the given order until MaxShown, skipping ones that are unavailable or already chosen
    private static void AddInOrder(
        List<RecommendedDishViewModel> chosen, IEnumerable<int> ids, Dictionary<int, RecommendedDishViewModel> available)
    {
        foreach (var id in ids)
        {
            if (chosen.Count >= MaxShown) break;
            if (available.TryGetValue(id, out var dish) && chosen.All(c => c.MenuItemId != id))
                chosen.Add(dish);
        }
    }

    // Fewer than MinShown looks lonely, so the row is hidden instead
    private static RecommendationViewModel Build(string title, List<RecommendedDishViewModel> chosen) =>
        chosen.Count < MinShown
            ? new RecommendationViewModel()
            : new RecommendationViewModel { Title = title, Dishes = chosen };
}
