// PB23 (Carynn) - customer activity report (interface)
using FoodOrderingSystem.ViewModels;   // <-- match your project namespace

namespace FoodOrderingSystem.Services;

public interface ICustomerActivityService
{
    /// <summary>
    /// PB23: per-customer activity between two dates (inclusive). sort: "active" (default) or "spend".
    /// Reads through IReservationService, IOrderService and IPaymentService only.
    /// </summary>
    Task<List<CustomerActivityRowViewModel>> GetCustomerActivityAsync(DateOnly from, DateOnly to, string? sort);
}
