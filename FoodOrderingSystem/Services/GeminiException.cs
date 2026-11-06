namespace FoodOrderingSystem.Services;

// Thrown by GeminiClient when the Gemini API refuses or fails a request.
// The message is already safe to show a customer: no key, no raw API error text.
public class GeminiException : Exception
{
    public GeminiException(string message, TimeSpan? retryAfter = null) : base(message)
    {
        RetryAfter = retryAfter;
    }

    // Set when Google said "too many requests, retry in N seconds" (free tier: 15 requests a minute)
    public TimeSpan? RetryAfter { get; }
}
