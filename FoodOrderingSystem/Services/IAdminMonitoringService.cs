// PB21 (Carynn) - admin monitoring (interface)
using FoodOrderingSystem.Models.Enums;   // <-- match your project namespace
using FoodOrderingSystem.ViewModels;

namespace FoodOrderingSystem.Services;

public interface IAdminMonitoringService
{
    // PB21 monitoring (read-only). All data comes through the other members' services.
    Task<ReservationMonitorViewModel> GetReservationsAsync(DateOnly from, DateOnly to, string? status);
    Task<OrderMonitorViewModel> GetOrdersAsync(DateOnly from, DateOnly to, OrderStatus? status);
    Task<PaymentMonitorViewModel> GetPaymentsAsync(DateOnly from, DateOnly to, PaymentStatus? status);
}
