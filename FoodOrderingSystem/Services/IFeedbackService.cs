using FoodOrderingSystem.ViewModels;

namespace FoodOrderingSystem.Services;

public interface IFeedbackService
{
    /// <summary>
    /// PB19: Returns completed reservations and completed orders belonging to the customer that do not have feedback yet.
    /// </summary>
    Task<List<EligibleVisitOptionViewModel>> GetEligibleVisitsAsync(string customerId);

    /// <summary>
    /// PB19: Submits a 1-5 rating and comment for a completed reservation or order owned by the customer (one per visit).
    /// </summary>
    Task<(bool Success, string Message)> SubmitFeedbackAsync(
        string customerId,
        int? reservationId,
        int? orderId,
        int rating,
        string? comment);

    /// <summary>
    /// PB19: Returns all feedback submitted by a specific customer.
    /// </summary>
    Task<List<FeedbackItemViewModel>> GetCustomerFeedbackAsync(string customerId);

    /// <summary>
    /// PB19: Returns all customer feedback for the Staff/Admin review list, with optional rating filter.
    /// </summary>
    Task<List<FeedbackItemViewModel>> GetAllFeedbackAsync(int? ratingFilter = null);
}