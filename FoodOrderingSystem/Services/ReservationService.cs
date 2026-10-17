using FoodOrderingSystem.Data;
using FoodOrderingSystem.Models.Enums;
using FoodOrderingSystem.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace FoodOrderingSystem.Services;

public class ReservationService : IReservationService
{
    private readonly ApplicationDbContext _context;

    // Business rules: 90-min dining slot + 15-min cleaning buffer
    public const int DiningDurationMinutes = 90;
    public const int TurnoverBufferMinutes = 15;
    public const int MinAdvanceBookingMinutes = 30;
    public const int MaxAdvanceBookingDays = 60;

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

        // Same-day past time or too close to current time check
        var requestedDateTime = date.ToDateTime(startTime);
        if (requestedDateTime < DateTime.Now.AddMinutes(MinAdvanceBookingMinutes))
            return (false, $"Reservations must be booked at least {MinAdvanceBookingMinutes} minutes in advance.");

        int dayOfWeek = (int)date.DayOfWeek; // 0 = Sunday .. 6 = Saturday
        var hours = await _context.OperatingHours
            .FirstOrDefaultAsync(h => h.DayOfWeek == dayOfWeek);

        if (hours == null || hours.IsClosed)
            return (false, "The restaurant is closed on the selected date.");

        // Guard against midnight wrap-around by comparing startTime against lastSeatingTime
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

        // Pre-calculate buffer bounds: [StartTime - 15m, EndTime + 15m]
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

        // Prevent a 1-2 person party from locking an 8-seater table
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
}