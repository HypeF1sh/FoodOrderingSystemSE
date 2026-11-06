// PB27 (Jordan) - AI chatbot (interface)
using FoodOrderingSystem.ViewModels;

namespace FoodOrderingSystem.Services;

// PB27 AI chatbot: answers customers' questions about the restaurant using the real menu, hours, offers and
// (when logged in) their own bookings and orders. Read-only: it never books, orders or changes anything.
public interface IChatbotService
{
    /// <summary>False when no Gemini API key is set on this PC; the widget then says the chatbot is not set up.</summary>
    bool IsAvailable { get; }

    /// <summary>Answers the last message in the conversation, yielding progress, answer text and errors as they happen.
    /// customerId is the logged-in customer (null for visitors), used only to look up their own bookings and orders.
    /// Cancelling the token (Stop button or closed page) stops the answer and the call to Gemini.</summary>
    IAsyncEnumerable<ChatStreamEvent> AnswerAsync(List<ChatMessageInput> messages, string? customerId,
        CancellationToken cancellationToken);
}
