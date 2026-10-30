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

    public async Task<List<EligibleVisitOptionViewModel>> GetEligibleVisitsAsync(string customerId)
    {
        var options = new List<EligibleVisitOptionViewModel>();

        var reviewedReservationIds = await _context.Feedbacks
            .Where(f => f.CustomerId == customerId && f.ReservationId.HasValue)
            .Select(f => f.ReservationId!.Value)
            .ToListAsync();

        var reviewedOrderIds = await _context.Feedbacks
            .Where(f => f.CustomerId == customerId && f.OrderId.HasValue)
            .Select(f => f.OrderId!.Value)
            .ToListAsync();

        var completedReservations = await _context.Reservations
            .Include(r => r.RestaurantTable)
            .Where(r => r.CustomerId == customerId
                        && r.Status == ReservationStatus.Completed
                        && !reviewedReservationIds.Contains(r.ReservationId))
            .OrderByDescending(r => r.ReservationDate)
            .ToListAsync();

        foreach (var r in completedReservations)
        {
            options.Add(new EligibleVisitOptionViewModel
            {
                ReservationId = r.ReservationId,
                DisplayLabel = $"Reservation #{r.ReservationId} — Table {r.RestaurantTable?.TableNumber} on {r.ReservationDate:dd/MM/yyyy} ({r.StartTime:hh\\:mm tt})"
            });
        }

        var completedOrders = await _context.Orders
            .Where(o => o.CustomerId == customerId
                        && o.Status == OrderStatus.Completed
                        && !reviewedOrderIds.Contains(o.OrderId))
            .OrderByDescending(o => o.CreatedAt)
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

    public async Task<(bool Success, string Message)> SubmitFeedbackAsync(
        string customerId,
        int? reservationId,
        int? orderId,
        int rating,
        string? comment)
    {
        if (rating < 1 || rating > 5)
            return (false, "Rating must be between 1 and 5 stars.");

        if (!reservationId.HasValue && !orderId.HasValue)
            return (false, "Please select a completed reservation or order to rate.");

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