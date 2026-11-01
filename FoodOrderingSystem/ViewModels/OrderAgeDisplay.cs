namespace FoodOrderingSystem.ViewModels;

// How long ago an order was placed, and its colour on The Pass.
// OrderService.PlaceOrderAsync saves Order.CreatedAt in restaurant local time (DateTime.Now, as agreed in the
// coding conventions), so it is compared with DateTime.Now, never with UtcNow.
// There is no "cooking started" or "served at" time on Order, so this is always time since it was placed.
public static class OrderAgeDisplay
{
    public const int AmberFromMinutes = 10;
    public const int PlumFromMinutes = 20;

    public static int MinutesSincePlaced(DateTime createdAt) =>
        Math.Max(0, (int)(DateTime.Now - createdAt).TotalMinutes);

    public static string ToneClass(int minutes) =>
        minutes >= PlumFromMinutes ? "is-plum"
        : minutes >= AmberFromMinutes ? "is-amber"
        : "is-jade";
}
