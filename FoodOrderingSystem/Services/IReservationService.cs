using FoodOrderingSystem.Models.Enums;
using FoodOrderingSystem.ViewModels;

namespace FoodOrderingSystem.Services;

public interface IReservationService
{
    Task<List<DiningAreaOptionViewModel>> GetDiningAreasAsync();

    Task<(bool IsOpen, string ErrorMessage)> ValidateOperatingHoursAsync(DateOnly date, TimeOnly startTime, bool bypassAdvanceNotice = false);

    Task<List<AvailableTableViewModel>> GetAvailableTablesAsync(
        DateOnly date,
        TimeOnly startTime,
        int partySize,
        int? diningAreaId = null,
        int? excludeReservationId = null,
        bool bypassAdvanceNotice = false);

    Task<(bool Success, string Message, int? ReservationId)> CreateReservationAsync(
        string customerId,
        int restaurantTableId,
        DateOnly date,
        TimeOnly startTime,
        int partySize,
        string? specialRequests);

    Task<ReservationDetailViewModel?> GetReservationByIdAsync(int reservationId, string? customerId = null);

    Task<List<ReservationDetailViewModel>> GetCustomerReservationsAsync(string customerId);

    Task<(bool Success, string Message)> UpdateReservationAsync(
        int reservationId,
        string customerId,
        int restaurantTableId,
        DateOnly date,
        TimeOnly startTime,
        int partySize,
        string? specialRequests);

    Task<(bool Success, string Message)> CancelReservationAsync(int reservationId, string customerId);

    // ---- Sprint 3: PB12 (Staff Daily Reservations & Management) ----

    Task<List<ReservationDetailViewModel>> GetStaffDailyReservationsAsync(
        DateOnly? date,
        string? statusFilter = null,
        int? diningAreaId = null,
        string? searchTerm = null);

    Task<(bool Success, string Message)> UpdateStatusByStaffAsync(
        int reservationId,
        ReservationStatus newStatus);

    Task<(bool Success, string Message)> UpdateReservationByStaffAsync(
        int reservationId,
        int restaurantTableId,
        DateOnly date,
        TimeOnly startTime,
        int partySize,
        ReservationStatus status,
        string? specialRequests);

    // ---- Sprint 3: PB13 (Staff Customer Requests, Preferences & History) ----

    Task<CustomerReservationProfileViewModel?> GetCustomerReservationProfileAsync(string customerId);

    Task<List<ReservationDetailViewModel>> GetReservationsWithRequestsAsync(DateOnly? fromDate = null);

    // ---- Sprint 4: Cross-domain query for PB21 (Admin Monitor) & PB22 (Dashboard Reports) ----

    Task<List<ReservationDetailViewModel>> GetReservationsAsync(
        DateOnly? from = null,
        DateOnly? to = null,
        ReservationStatus? status = null);
}