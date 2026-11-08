// PB19 (Wallace) - feedback: who can rate, save, staff list
using FoodOrderingSystem.Data;
using FoodOrderingSystem.Models;
using FoodOrderingSystem.Models.Enums;
using FoodOrderingSystem.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace FoodOrderingSystem.Services;

public class FeedbackService : IFeedbackService
{
    private readonly ApplicationDbContext _context;

    public FeedbackService(ApplicationDbContext context)
    {
        _context = context;
    }

    // PB19: visits the customer may still rate = Completed bookings and orders not rated yet
    public async Task<List<EligibleVisitOptionViewModel>> GetEligibleVisitsAsync(string customerId)
    {
        var options = new List<EligibleVisitOptionViewModel>();

        if (string.IsNullOrWhiteSpace(customerId))
            return options;

        // 1. Completed bookings they have not rated yet (single SQL query, no Contains list bug)
        var completedReservations = await _context.Reservations
            .AsNoTracking()
            .Where(r => r.CustomerId == customerId
                        && r.Status == ReservationStatus.Completed
                        && !_context.Feedbacks.Any(f => f.ReservationId == r.ReservationId))
            .OrderByDescending(r => r.ReservationDate)
            .Select(r => new
            {
                r.ReservationId,
                TableNumber = r.RestaurantTable != null ? r.RestaurantTable.TableNumber : "N/A",
                r.ReservationDate,
                r.StartTime
            })
            .ToListAsync();

        foreach (var r in completedReservations)
        {
            options.Add(new EligibleVisitOptionViewModel
            {
                ReservationId = r.ReservationId,
                DisplayLabel = $"Reservation #{r.ReservationId} — Table {r.TableNumber} on {r.ReservationDate:dd/MM/yyyy} ({r.StartTime:hh\\:mm tt})"
            });
        }

        // 2. Completed orders they have not rated yet
        var completedOrders = await _context.Orders
            .AsNoTracking()
            .Where(o => o.CustomerId == customerId
                        && o.Status == OrderStatus.Completed
                        && !_context.Feedbacks.Any(f => f.OrderId == o.OrderId))
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new
            {
                o.OrderId,
                o.TotalAmount,
                o.CreatedAt
            })
            .ToListAsync();

        foreach (var o in completedOrders)
        {
            options.Add(new EligibleVisitOptionViewModel
            {
                OrderId = o.OrderId,
                DisplayLabel = $"Order #{o.OrderId} — RM {o.TotalAmount:0.00} on {o.CreatedAt.ToLocalTime():dd/MM/yyyy hh:mm tt}"
            });
        }

        return options;
    }

    // PB19: save a rating; every rule checked on the server
    public async Task<(bool Success, string Message)> SubmitFeedbackAsync(
        string customerId,
        int? reservationId,
        int? orderId,
        int rating,
        string? comment)
    {
        // 1. 1-5 stars, and it must be about a booking or an order
        if (rating < 1 || rating > 5)
            return (false, "Rating must be between 1 and 5 stars.");

        if (!reservationId.HasValue && !orderId.HasValue)
            return (false, "Please select a completed reservation or order to rate.");

        // 2. Booking: must be theirs, Completed, and not rated before
        if (reservationId.HasValue)
        {
            var reservation = await _context.Reservations
                .FirstOrDefaultAsync(r => r.ReservationId == reservationId.Value && r.CustomerId == customerId);

            if (reservation == null)
                return (false, "Reservation not found or does not belong to your account.");

            if (reservation.Status != ReservationStatus.Completed)
                return (false, "You can only leave feedback after a reservation is marked as Completed.");

            bool alreadyReviewed = await _context.Feedbacks
                .AnyAsync(f => f.ReservationId == reservationId.Value);

            if (alreadyReviewed)
                return (false, "You have already submitted feedback for this reservation.");
        }

        // 3. Order: same three checks
        if (orderId.HasValue)
        {
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.OrderId == orderId.Value && o.CustomerId == customerId);

            if (order == null)
                return (false, "Order not found or does not belong to your account.");

            if (order.Status != OrderStatus.Completed)
                return (false, "You can only leave feedback after an order is marked as Completed.");

            bool alreadyReviewed = await _context.Feedbacks
                .AnyAsync(f => f.OrderId == orderId.Value);

            if (alreadyReviewed)
                return (false, "You have already submitted feedback for this order.");
        }

        // 4. Save the feedback
        var feedback = new Feedback
        {
            CustomerId = customerId,
            ReservationId = reservationId,
            OrderId = orderId,
            Rating = rating,
            Comment = comment?.Trim() ?? string.Empty,
            CreatedAt = DateTime.UtcNow
        };

        _context.Feedbacks.Add(feedback);
        await _context.SaveChangesAsync();

        return (true, "Thank you! Your rating and feedback have been submitted.");
    }

    // PB19: a customer's own past feedback
    public async Task<List<FeedbackItemViewModel>> GetCustomerFeedbackAsync(string customerId)
    {
        return await _context.Feedbacks
            .Include(f => f.Customer)
            .Where(f => f.CustomerId == customerId)
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => new FeedbackItemViewModel
            {
                FeedbackId = f.FeedbackId,
                CustomerId = f.CustomerId,
                CustomerName = f.Customer != null ? f.Customer.FullName : string.Empty,
                CustomerEmail = f.Customer != null ? (f.Customer.Email ?? string.Empty) : string.Empty,
                ReservationId = f.ReservationId,
                OrderId = f.OrderId,
                Rating = f.Rating,
                Comment = f.Comment,
                CreatedAt = f.CreatedAt
            })
            .ToListAsync();
    }

    // PB19: all feedback for staff, optional star filter
    public async Task<List<FeedbackItemViewModel>> GetAllFeedbackAsync(int? ratingFilter = null)
    {
        var query = _context.Feedbacks
            .Include(f => f.Customer)
            .AsQueryable();

        if (ratingFilter.HasValue && ratingFilter.Value >= 1 && ratingFilter.Value <= 5)
        {
            query = query.Where(f => f.Rating == ratingFilter.Value);
        }

        return await query
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => new FeedbackItemViewModel
            {
                FeedbackId = f.FeedbackId,
                CustomerId = f.CustomerId,
                CustomerName = f.Customer != null ? f.Customer.FullName : string.Empty,
                CustomerEmail = f.Customer != null ? (f.Customer.Email ?? string.Empty) : string.Empty,
                ReservationId = f.ReservationId,
                OrderId = f.OrderId,
                Rating = f.Rating,
                Comment = f.Comment,
                CreatedAt = f.CreatedAt
            })
            .ToListAsync();
    }
}