// PB14 (Jordan) - cart in the session (interface)
using FoodOrderingSystem.ViewModels;
using Microsoft.AspNetCore.Http;

namespace FoodOrderingSystem.Services;

// The customer's cart, kept in the server-side session (PB14).
// The page passes in HttpContext.Session, so the service has no hidden dependency on the request.
public interface ICartService
{
    /// <summary>The cart with each dish's current name, price, photo and availability.
    /// Lines for dishes that were deleted are dropped.</summary>
    Task<CartViewModel> GetCartAsync(ISession session);

    /// <summary>The raw cart lines (id + quantity) as stored in the session.</summary>
    List<CartSessionItem> GetItems(ISession session);

    /// <summary>Total number of dishes in the cart, for the "View cart" bar.</summary>
    int GetItemCount(ISession session);

    /// <summary>Adds an available dish, or increases its quantity if it is already in the cart.
    /// Returns (success, message).</summary>
    Task<(bool Success, string Message)> AddItemAsync(ISession session, int menuItemId, int quantity);

    /// <summary>Changes a line's quantity by delta (+1 / -1). Reaching 0 removes the line.
    /// Returns (success, message).</summary>
    (bool Success, string Message) ChangeQuantity(ISession session, int menuItemId, int delta);

    /// <summary>Removes one dish from the cart.</summary>
    void RemoveItem(ISession session, int menuItemId);

    /// <summary>Empties the cart (after an order is placed).</summary>
    void Clear(ISession session);
}
