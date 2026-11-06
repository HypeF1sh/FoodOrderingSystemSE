using System.Text.Json.Nodes;

namespace FoodOrderingSystem.Services;

// PB27: the only class that talks to Google's Gemini API. It knows HTTP and JSON, nothing about the restaurant -
// the rules and the restaurant data are in ChatbotService. Like a data-layer class, but for an outside service.
public interface IGeminiClient
{
    /// <summary>False when no API key is set on this PC, so the chatbot can say so instead of failing.</summary>
    bool IsConfigured { get; }

    /// <summary>Sends one request to streamGenerateContent and yields each streamed chunk as it arrives.
    /// Cancelling the token (the customer pressed Stop or closed the page) closes the connection to Google.
    /// Throws GeminiException with a customer-safe message if the API refuses the request.</summary>
    IAsyncEnumerable<JsonObject> StreamAsync(JsonObject request, CancellationToken cancellationToken);
}
