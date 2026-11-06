namespace FoodOrderingSystem.ViewModels;

// Data for the shared back office empty state (Pages/Shared/_EmptyState.cshtml).
// Icon is one of: calendar, receipt, star, ticket, table.
public record EmptyStateViewModel(string Title, string Text, string Icon = "calendar");
