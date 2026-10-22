using FoodOrderingSystem.Data;
using FoodOrderingSystem.Models;
using FoodOrderingSystem.Models.Enums;
using FoodOrderingSystem.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace FoodOrderingSystem.Services;

public class ReservationService : IReservationService
{
    private readonly ApplicationDbContext _context;

    public const int DiningDurationMinutes = 90;
    public const int TurnoverBufferMinutes = 15;
    public const int MinAdvanceBookingMinutes = 30;
    public const int MaxAdvanceBookingDays = 60;
    public const int CutoffHoursBeforeStart = 2;

    public ReservationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<DiningAreaOptionViewModel>> GetDiningAreasAsync()
    {
        return await _context.DiningAreas
            .OrderBy(d => d.DiningAreaId)
            .Select(d => new DiningAreaOptionViewModel
            {
                DiningAreaId = d.DiningAreaId,
                Name = d.Name,
                Description = d.Description
            })
            .ToListAsync();
    }

    public async Task<(bool IsOpen, string ErrorMessage)> ValidateOperatingHoursAsync(DateOnly date, TimeOnly startTime)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        if (date < today)
            return (false, "Reservation date cannot be in the past.");

        if (date > today.AddDays(MaxAdvanceBookingDays))
            return (false, $"Reservations can only be made up to {MaxAdvanceBookingDays} days in advance.");

        var requestedDateTime = date.ToDateTime(startTime);
        if (requestedDateTime < DateTime.Now.AddMinutes(MinAdvanceBookingMinutes))
            return (false, $"Reservations must be booked at least {MinAdvanceBookingMinutes} minutes in advance.");

        int dayOfWeek = (int)date.DayOfWeek;
        var hours = await _context.OperatingHours
            .FirstOrDefaultAsync(h => h.DayOfWeek == dayOfWeek);

        if (hours == null || hours.IsClosed)
            return (false, "The restaurant is closed on the selected date.");

        var lastSeatingTime = hours.CloseTime.AddMinutes(-DiningDurationMinutes);

        if (startTime < hours.OpenTime || startTime > lastSeatingTime)
        {
            return (false,
                $"Reservations on {date:dddd} must start between {hours.OpenTime:HH:mm} and {lastSeatingTime:HH:mm} " +
                $"(allowing a {DiningDurationMinutes}-minute dining slot before closing at {hours.CloseTime:HH:mm}).");
        }

        return (true, string.Empty);
    }

    public async Task<List<AvailableTableViewModel>> GetAvailableTablesAsync(
        DateOnly date,
        TimeOnly startTime,
        int partySize,
        int? diningAreaId = null,
        int? excludeReservationId = null)
    {
        if (partySize < 1 || partySize > 20)
            return new List<AvailableTableViewModel>();

        var (isOpen, _) = await ValidateOperatingHoursAsync(date, startTime);
        if (!isOpen)
            return new List<AvailableTableViewModel>();

        var requestedEnd = startTime.AddMinutes(DiningDurationMinutes);
        var windowStart = startTime.AddMinutes(-TurnoverBufferMinutes);
        var windowEnd = requestedEnd.AddMinutes(TurnoverBufferMinutes);

        var conflictingTableIds = await _context.Reservations
            .Where(r => r.ReservationDate == date
                        && r.Status != ReservationStatus.Cancelled
                        && r.Status != ReservationStatus.NoShow
                        && (!excludeReservationId.HasValue || r.ReservationId != excludeReservationId.Value)
                        && r.StartTime < windowEnd
                        && r.EndTime > windowStart)
            .Select(r => r.RestaurantTableId)
            .Distinct()
            .ToListAsync();

        int maxAllowedCapacity = Math.Max(partySize + 3, 4);

        var query = _context.RestaurantTables
            .Include(t => t.DiningArea)
            .Where(t => t.IsActive
                        && t.Capacity >= partySize
                        && t.Capacity <= maxAllowedCapacity
                        && !conflictingTableIds.Contains(t.RestaurantTableId));

        if (diningAreaId.HasValue && diningAreaId.Value > 0)
        {
            query = query.Where(t => t.DiningAreaId == diningAreaId.Value);
        }

        return await query
            .OrderBy(t => t.Capacity)
            .ThenBy(t => t.TableNumber)
            .Select(t => new AvailableTableViewModel
            {
                RestaurantTableId = t.RestaurantTableId,
                TableNumber = t.TableNumber,
                Capacity = t.Capacity,
                DiningAreaId = t.DiningAreaId,
                DiningAreaName = t.DiningArea != null ? t.DiningArea.Name : string.Empty
            })
            .ToListAsync();
    }

    public async Task<(bool Success, string Message, int? ReservationId)> CreateReservationAsync(
        string customerId,
        int restaurantTableId,
        DateOnly date,
        TimeOnly startTime,
        int partySize,
        string? specialRequests)
    {
        var (isOpen, hoursError) = await ValidateOperatingHoursAsync(date, startTime);
        if (!isOpen)
            return (false, hoursError, null);

        var requestedEnd = startTime.AddMinutes(DiningDurationMinutes);

        bool customerHasOverlappingBooking = await _context.Reservations
            .AnyAsync(r => r.CustomerId == customerId
                           && r.ReservationDate == date
                           && r.Status != ReservationStatus.Cancelled
                           && r.Status != ReservationStatus.NoShow
                           && r.StartTime < requestedEnd
                           && r.EndTime > startTime);

        if (customerHasOverlappingBooking)
            return (false, "You already have an active reservation during this time window.", null);

        var availableTables = await GetAvailableTablesAsync(date, startTime, partySize);
        var selectedTable = availableTables.FirstOrDefault(t => t.RestaurantTableId == restaurantTableId);

        if (selectedTable == null)
            return (false, "That table is no longer available for the selected time and party size. Please choose another table.", null);

        var reservation = new Reservation
        {
            CustomerId = customerId,
            RestaurantTableId = restaurantTableId,
            ReservationDate = date,
            StartTime = startTime,
            EndTime = requestedEnd,
            PartySize = partySize,
            Status = ReservationStatus.Confirmed,
            SpecialRequests = specialRequests?.Trim() ?? string.Empty,
            CreatedAt = DateTime.UtcNow
        };

        _context.Reservations.Add(reservation);
        await _context.SaveChangesAsync();

        return (true, $"Reservation confirmed for Table {selectedTable.TableNumber} on {date:dd MMM yyyy} at {startTime:HH:mm}.", reservation.ReservationId);
    }

    public async Task<ReservationDetailViewModel?> GetReservationByIdAsync(int reservationId, string? customerId = null)
    {
        var query = _context.Reservations
            .Include(r => r.RestaurantTable)
                .ThenInclude(t => t!.DiningArea)
            .Include(r => r.Customer)
            .Where(r => r.ReservationId == reservationId);

        if (!string.IsNullOrEmpty(customerId))
        {
            query = query.Where(r => r.CustomerId == customerId);
        }

        var r = await query.FirstOrDefaultAsync();
        return r == null ? null : MapToDetailViewModel(r);
    }

    public async Task<List<ReservationDetailViewModel>> GetCustomerReservationsAsync(string customerId)
    {
        var list = await _context.Reservations
            .Include(r => r.RestaurantTable)
                .ThenInclude(t => t!.DiningArea)
            .Include(r => r.Customer)
            .Where(r => r.CustomerId == customerId)
            .OrderByDescending(r => r.ReservationDate)
            .ThenByDescending(r => r.StartTime)
            .ToListAsync();

        return list.Select(MapToDetailViewModel).ToList();
    }

    public async Task<(bool Success, string Message)> UpdateReservationAsync(
        int reservationId,
        string customerId,
        int restaurantTableId,
        DateOnly date,
        TimeOnly startTime,
        int partySize,
        string? specialRequests)
    {
        var reservation = await _context.Reservations
            .FirstOrDefaultAsync(r => r.ReservationId == reservationId && r.CustomerId == customerId);

        if (reservation == null)
            return (false, "Reservation not found.");

        var (canModify, lockReason) = EvaluateModificationPolicy(reservation.Status, reservation.ReservationDate, reservation.StartTime);
        if (!canModify)
            return (false, lockReason);

        var (isOpen, hoursError) = await ValidateOperatingHoursAsync(date, startTime);
        if (!isOpen)
            return (false, hoursError);

        var requestedEnd = startTime.AddMinutes(DiningDurationMinutes);

        bool customerHasOtherOverlap = await _context.Reservations
            .AnyAsync(r => r.CustomerId == customerId
                           && r.ReservationId != reservationId
                           && r.ReservationDate == date
                           && r.Status != ReservationStatus.Cancelled
                           && r.Status != ReservationStatus.NoShow
                           && r.StartTime < requestedEnd
                           && r.EndTime > startTime);

        if (customerHasOtherOverlap)
            return (false, "You already have another active reservation during that new time window.");

        var availableTables = await GetAvailableTablesAsync(date, startTime, partySize, null, reservationId);
        var selectedTable = availableTables.FirstOrDefault(t => t.RestaurantTableId == restaurantTableId);

        if (selectedTable == null)
            return (false, "The selected table is not available or cannot fit the updated party size for that time slot.");

        reservation.RestaurantTableId = restaurantTableId;
        reservation.ReservationDate = date;
        reservation.StartTime = startTime;
        reservation.EndTime = requestedEnd;
        reservation.PartySize = partySize;
        reservation.SpecialRequests = specialRequests?.Trim() ?? string.Empty;

        await _context.SaveChangesAsync();
        return (true, $"Reservation #{reservationId} updated successfully.");
    }

    public async Task<(bool Success, string Message)> CancelReservationAsync(int reservationId, string customerId)
    {
        var reservation = await _context.Reservations
            .FirstOrDefaultAsync(r => r.ReservationId == reservationId && r.CustomerId == customerId);

        if (reservation == null)
            return (false, "Reservation not found.");

        var (canCancel, lockReason) = EvaluateModificationPolicy(reservation.Status, reservation.ReservationDate, reservation.StartTime);
        if (!canCancel)
            return (false, lockReason);

        reservation.Status = ReservationStatus.Cancelled;
        await _context.SaveChangesAsync();

        return (true, $"Reservation #{reservationId} has been cancelled.");
    }

    private static (bool Allowed, string Reason) EvaluateModificationPolicy(
        ReservationStatus status, DateOnly date, TimeOnly startTime)
    {
        if (status == ReservationStatus.Cancelled)
            return (false, "This reservation is already cancelled.");

        if (status == ReservationStatus.Completed || status == ReservationStatus.NoShow)
            return (false, $"Reservations marked as '{status}' cannot be modified or cancelled.");

        var reservationStartDateTime = date.ToDateTime(startTime);
        if (reservationStartDateTime <= DateTime.Now)
            return (false, "Past or ongoing reservations cannot be modified or cancelled.");

        if (reservationStartDateTime < DateTime.Now.AddHours(CutoffHoursBeforeStart))
        {
            return (false, $"Changes or cancellations must be made at least {CutoffHoursBeforeStart} hours before the reservation start time. Please contact restaurant staff directly.");
        }

        return (true, string.Empty);
    }

    private static ReservationDetailViewModel MapToDetailViewModel(Reservation r)
    {
        var (canModify, lockReason) = EvaluateModificationPolicy(r.Status, r.ReservationDate, r.StartTime);

        return new ReservationDetailViewModel
        {
            ReservationId = r.ReservationId,
            CustomerId = r.CustomerId,
            CustomerName = r.Customer?.FullName ?? string.Empty,
            CustomerEmail = r.Customer?.Email ?? string.Empty,
            RestaurantTableId = r.RestaurantTableId,
            TableNumber = r.RestaurantTable?.TableNumber ?? string.Empty,
            TableCapacity = r.RestaurantTable?.Capacity ?? 0,
            DiningAreaName = r.RestaurantTable?.DiningArea?.Name ?? string.Empty,
            ReservationDate = r.ReservationDate,
            StartTime = r.StartTime,
            EndTime = r.EndTime,
            PartySize = r.PartySize,
            Status = r.Status.ToString(),
            SpecialRequests = r.SpecialRequests,
            CreatedAt = r.CreatedAt,
            CanModifyOrCancel = canModify,
            LockReason = canModify ? null : lockReason
        };
    }
}