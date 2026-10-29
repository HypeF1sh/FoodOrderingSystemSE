using FoodOrderingSystem.Data;
using FoodOrderingSystem.Models;
using FoodOrderingSystem.Models.Enums;
using FoodOrderingSystem.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace FoodOrderingSystem.Services;

public class ReservationService : IReservationService
{
    private readonly ApplicationDbContext _context;
    private readonly IDiningAreaService _diningAreaService;
    private readonly IOperatingHoursService _operatingHoursService;
    private readonly INotificationService _notificationService;

    public const int DiningDurationMinutes = 90;
    public const int TurnoverBufferMinutes = 15;
    public const int MinAdvanceBookingMinutes = 30;
    public const int MaxAdvanceBookingDays = 60;
    public const int CutoffHoursBeforeStart = 2;
    public const int MaxOnlinePartySize = 8;

    public ReservationService(
        ApplicationDbContext context,
        IDiningAreaService diningAreaService,
        IOperatingHoursService operatingHoursService,
        INotificationService notificationService)
    {
        _context = context;
        _diningAreaService = diningAreaService;
        _operatingHoursService = operatingHoursService;
        _notificationService = notificationService;
    }

    public async Task<List<DiningAreaOptionViewModel>> GetDiningAreasAsync()
    {
        var areas = await _diningAreaService.GetAreasAsync();
        return areas
            .OrderBy(d => d.DiningAreaId)
            .Select(d => new DiningAreaOptionViewModel
            {
                DiningAreaId = d.DiningAreaId,
                Name = d.Name,
                Description = d.Description
            })
            .ToList();
    }

    public async Task<(bool IsOpen, string ErrorMessage)> ValidateOperatingHoursAsync(
        DateOnly date, TimeOnly startTime, bool bypassAdvanceNotice = false)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        if (!bypassAdvanceNotice)
        {
            if (date < today)
                return (false, "Reservation date cannot be in the past.");

            if (date > today.AddDays(MaxAdvanceBookingDays))
                return (false, $"Reservations can only be made up to {MaxAdvanceBookingDays} days in advance.");

            var requestedDateTime = date.ToDateTime(startTime);
            if (requestedDateTime < DateTime.Now.AddMinutes(MinAdvanceBookingMinutes))
                return (false, $"Reservations must be booked at least {MinAdvanceBookingMinutes} minutes in advance.");
        }

        int dayOfWeek = (int)date.DayOfWeek;
        var week = await _operatingHoursService.GetWeekAsync();
        var hours = week.FirstOrDefault(h => (int)h.DayOfWeek == dayOfWeek);

        if (hours == null || hours.IsClosed)
            return (false, "The restaurant is closed on the selected date.");

        var lastSeatingTime = hours.CloseTime.AddMinutes(-DiningDurationMinutes);

        if (startTime < hours.OpenTime || startTime > lastSeatingTime)
        {
            return (false,
                $"Reservations on {date:dddd} must start between {hours.OpenTime:hh\\:mm tt} and {lastSeatingTime:hh\\:mm tt} " +
                $"(allowing a {DiningDurationMinutes}-minute dining slot before closing at {hours.CloseTime:hh\\:mm tt}).");
        }

        return (true, string.Empty);
    }

    public async Task<List<AvailableTableViewModel>> GetAvailableTablesAsync(
        DateOnly date,
        TimeOnly startTime,
        int partySize,
        int? diningAreaId = null,
        int? excludeReservationId = null,
        bool bypassAdvanceNotice = false)
    {
        if (partySize < 1 || partySize > MaxOnlinePartySize)
            return new List<AvailableTableViewModel>();

        var (isOpen, _) = await ValidateOperatingHoursAsync(date, startTime, bypassAdvanceNotice);
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

        var allTables = await _diningAreaService.GetTablesAsync();

        return allTables
            .Where(t => t.IsActive
                        && t.Capacity >= partySize
                        && (bypassAdvanceNotice || t.Capacity <= maxAllowedCapacity)
                        && !conflictingTableIds.Contains(t.RestaurantTableId)
                        && (!diningAreaId.HasValue || diningAreaId.Value <= 0 || t.DiningAreaId == diningAreaId.Value))
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
            .ToList();
    }

    public async Task<(bool Success, string Message, int? ReservationId)> CreateReservationAsync(
        string customerId,
        int restaurantTableId,
        DateOnly date,
        TimeOnly startTime,
        int partySize,
        string? specialRequests)
    {
        if (partySize < 1 || partySize > MaxOnlinePartySize)
            return (false, $"Online bookings are limited to 1–{MaxOnlinePartySize} guests. Please call us directly for larger groups.", null);

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
            Status = ReservationStatus.Pending,
            SpecialRequests = specialRequests?.Trim() ?? string.Empty,
            CreatedAt = DateTime.UtcNow
        };

        _context.Reservations.Add(reservation);
        await _context.SaveChangesAsync();

        // PB20: Notify customer of reservation submission
        await _notificationService.SendAsync(
            customerId,
            "Reservation",
            $"Reservation #{reservation.ReservationId} for Table {selectedTable.TableNumber} on {date:dd/MM/yyyy} at {startTime:hh\\:mm tt} has been submitted and is pending staff confirmation.");

        return (true,
            $"Reservation request submitted for Table {selectedTable.TableNumber} on {date:dd/MM/yyyy} at {startTime:hh\\:mm tt} (Pending staff confirmation).",
            reservation.ReservationId);
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
        if (partySize < 1 || partySize > MaxOnlinePartySize)
            return (false, $"Online bookings are limited to 1–{MaxOnlinePartySize} guests. Please call us directly for larger groups.");

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
        return (true, $"Reservation #{reservationId} updated for {date:dd/MM/yyyy} at {startTime:hh\\:mm tt}.");
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

        // PB20: Notify customer of cancellation
        await _notificationService.SendAsync(
            customerId,
            "Reservation",
            $"Your Reservation #{reservationId} on {reservation.ReservationDate:dd/MM/yyyy} at {reservation.StartTime:hh\\:mm tt} has been cancelled.");

        return (true, $"Reservation #{reservationId} has been cancelled.");
    }

    // ---- Sprint 3: PB12 (Staff Daily Reservations & Management) ----

    public async Task<List<ReservationDetailViewModel>> GetStaffDailyReservationsAsync(
        DateOnly? date,
        string? statusFilter = null,
        int? diningAreaId = null,
        string? searchTerm = null)
    {
        var query = _context.Reservations
            .Include(r => r.RestaurantTable)
                .ThenInclude(t => t!.DiningArea)
            .Include(r => r.Customer)
            .AsQueryable();

        if (date.HasValue)
        {
            query = query.Where(r => r.ReservationDate == date.Value);
        }

        if (!string.IsNullOrWhiteSpace(statusFilter) &&
            Enum.TryParse<ReservationStatus>(statusFilter, true, out var parsedStatus))
        {
            query = query.Where(r => r.Status == parsedStatus);
        }

        if (diningAreaId.HasValue && diningAreaId.Value > 0)
        {
            query = query.Where(r => r.RestaurantTable != null && r.RestaurantTable.DiningAreaId == diningAreaId.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(r =>
                (r.Customer != null && (r.Customer.FullName.ToLower().Contains(term) || (r.Customer.Email != null && r.Customer.Email.ToLower().Contains(term)))) ||
                (r.RestaurantTable != null && r.RestaurantTable.TableNumber.ToLower().Contains(term)) ||
                r.SpecialRequests.ToLower().Contains(term));
        }

        var list = await query
            .OrderBy(r => r.ReservationDate)
            .ThenBy(r => r.StartTime)
            .ThenBy(r => r.RestaurantTable!.TableNumber)
            .ToListAsync();

        return list.Select(MapToDetailViewModel).ToList();
    }

    public async Task<(bool Success, string Message)> UpdateStatusByStaffAsync(
        int reservationId,
        ReservationStatus newStatus)
    {
        var reservation = await _context.Reservations
            .Include(r => r.RestaurantTable)
            .FirstOrDefaultAsync(r => r.ReservationId == reservationId);

        if (reservation == null)
            return (false, "Reservation not found.");

        if (reservation.Status == newStatus)
            return (false, $"Reservation #{reservationId} is already marked as {newStatus}.");

        if ((reservation.Status == ReservationStatus.Cancelled || reservation.Status == ReservationStatus.NoShow) &&
            (newStatus == ReservationStatus.Confirmed || newStatus == ReservationStatus.Pending))
        {
            var availableTables = await GetAvailableTablesAsync(
                reservation.ReservationDate,
                reservation.StartTime,
                reservation.PartySize,
                null,
                reservation.ReservationId,
                bypassAdvanceNotice: true);

            if (!availableTables.Any(t => t.RestaurantTableId == reservation.RestaurantTableId))
            {
                return (false, $"Cannot reactivate Reservation #{reservationId} because Table {reservation.RestaurantTable?.TableNumber} is now booked by another party during that slot.");
            }
        }

        reservation.Status = newStatus;
        await _context.SaveChangesAsync();

        // PB20: Notify customer when staff confirms, cancels, or updates their booking status
        if (newStatus == ReservationStatus.Confirmed || newStatus == ReservationStatus.Cancelled || newStatus == ReservationStatus.Completed)
        {
            var actionText = newStatus switch
            {
                ReservationStatus.Confirmed => "has been confirmed by our staff",
                ReservationStatus.Cancelled => "has been cancelled by our staff",
                ReservationStatus.Completed => "is now marked as completed. Thank you for dining with us! You can now leave a rating and feedback",
                _ => $"status was updated to {newStatus}"
            };

            await _notificationService.SendAsync(
                reservation.CustomerId,
                "Reservation",
                $"Reservation #{reservation.ReservationId} (Table {reservation.RestaurantTable?.TableNumber} on {reservation.ReservationDate:dd/MM/yyyy} at {reservation.StartTime:hh\\:mm tt}) {actionText}.");
        }

        return (true, $"Reservation #{reservationId} status updated to {newStatus}.");
    }

    public async Task<(bool Success, string Message)> UpdateReservationByStaffAsync(
        int reservationId,
        int restaurantTableId,
        DateOnly date,
        TimeOnly startTime,
        int partySize,
        ReservationStatus status,
        string? specialRequests)
    {
        if (partySize < 1 || partySize > MaxOnlinePartySize)
            return (false, $"Party size must be between 1 and {MaxOnlinePartySize}.");

        var reservation = await _context.Reservations
            .FirstOrDefaultAsync(r => r.ReservationId == reservationId);

        if (reservation == null)
            return (false, "Reservation not found.");

        var previousStatus = reservation.Status;

        var (isOpen, hoursError) = await ValidateOperatingHoursAsync(date, startTime, bypassAdvanceNotice: true);
        if (!isOpen)
            return (false, hoursError);

        if (status != ReservationStatus.Cancelled && status != ReservationStatus.NoShow)
        {
            var availableTables = await GetAvailableTablesAsync(
                date, startTime, partySize, null, reservationId, bypassAdvanceNotice: true);

            if (!availableTables.Any(t => t.RestaurantTableId == restaurantTableId))
            {
                return (false, "The selected table is either occupied during that slot or does not have enough capacity for the party size.");
            }
        }

        reservation.RestaurantTableId = restaurantTableId;
        reservation.ReservationDate = date;
        reservation.StartTime = startTime;
        reservation.EndTime = startTime.AddMinutes(DiningDurationMinutes);
        reservation.PartySize = partySize;
        reservation.Status = status;
        reservation.SpecialRequests = specialRequests?.Trim() ?? string.Empty;

        await _context.SaveChangesAsync();

        if (previousStatus != status && (status == ReservationStatus.Confirmed || status == ReservationStatus.Cancelled))
        {
            await _notificationService.SendAsync(
                reservation.CustomerId,
                "Reservation",
                $"Your Reservation #{reservationId} for {date:dd/MM/yyyy} at {startTime:hh\\:mm tt} has been updated to {status}.");
        }

        return (true, $"Reservation #{reservationId} updated by staff.");
    }

    // ---- Sprint 3: PB13 (Staff Customer Requests, Preferences & History) ----

    public async Task<CustomerReservationProfileViewModel?> GetCustomerReservationProfileAsync(string customerId)
    {
        var customer = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == customerId);

        if (customer == null)
            return null;

        var reservations = await _context.Reservations
            .Include(r => r.RestaurantTable)
                .ThenInclude(t => t!.DiningArea)
            .Include(r => r.Customer)
            .Where(r => r.CustomerId == customerId)
            .OrderByDescending(r => r.ReservationDate)
            .ThenByDescending(r => r.StartTime)
            .ToListAsync();

        var mapped = reservations.Select(MapToDetailViewModel).ToList();

        var activeOrCompleted = mapped.Where(r => r.Status != "Cancelled").ToList();
        var preferredArea = activeOrCompleted
            .Where(r => !string.IsNullOrEmpty(r.DiningAreaName))
            .GroupBy(r => r.DiningAreaName)
            .OrderByDescending(g => g.Count())
            .Select(g => $"{g.Key} ({g.Count()} visit(s))")
            .FirstOrDefault() ?? "No preference recorded yet";

        var typicalPartySize = activeOrCompleted.Any()
            ? (int)Math.Round(activeOrCompleted.Average(r => r.PartySize))
            : 0;

        var notableRequests = mapped
            .Where(r => !string.IsNullOrWhiteSpace(r.SpecialRequests))
            .Select(r => $"{r.ReservationDate:dd/MM/yyyy}: \"{r.SpecialRequests}\"")
            .Distinct()
            .Take(8)
            .ToList();

        return new CustomerReservationProfileViewModel
        {
            CustomerId = customer.Id,
            CustomerName = customer.FullName,
            CustomerEmail = customer.Email ?? string.Empty,
            TotalBookings = mapped.Count,
            CompletedCount = mapped.Count(r => r.Status == "Completed"),
            CancelledCount = mapped.Count(r => r.Status == "Cancelled"),
            NoShowCount = mapped.Count(r => r.Status == "NoShow"),
            PreferredDiningArea = preferredArea,
            TypicalPartySize = typicalPartySize,
            NotableRequests = notableRequests,
            Reservations = mapped
        };
    }

    public async Task<List<ReservationDetailViewModel>> GetReservationsWithRequestsAsync(DateOnly? fromDate = null)
    {
        var query = _context.Reservations
            .Include(r => r.RestaurantTable)
                .ThenInclude(t => t!.DiningArea)
            .Include(r => r.Customer)
            .Where(r => r.SpecialRequests != "");

        if (fromDate.HasValue)
        {
            query = query.Where(r => r.ReservationDate >= fromDate.Value);
        }

        var list = await query
            .OrderBy(r => r.ReservationDate)
            .ThenBy(r => r.StartTime)
            .ToListAsync();

        return list.Select(MapToDetailViewModel).ToList();
    }

    // ---- Sprint 4: Cross-domain query for PB21 & PB22 ----

    public async Task<List<ReservationDetailViewModel>> GetReservationsAsync(
        DateOnly? from = null,
        DateOnly? to = null,
        ReservationStatus? status = null)
    {
        var query = _context.Reservations
            .Include(r => r.RestaurantTable)
                .ThenInclude(t => t!.DiningArea)
            .Include(r => r.Customer)
            .AsQueryable();

        if (from.HasValue)
        {
            query = query.Where(r => r.ReservationDate >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(r => r.ReservationDate <= to.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        var list = await query
            .OrderByDescending(r => r.ReservationDate)
            .ThenByDescending(r => r.StartTime)
            .ToListAsync();

        return list.Select(MapToDetailViewModel).ToList();
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
            DiningAreaId = r.RestaurantTable?.DiningAreaId ?? 0,
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