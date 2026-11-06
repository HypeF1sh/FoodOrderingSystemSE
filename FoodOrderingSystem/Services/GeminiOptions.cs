// PB27 (Jordan) - Gemini settings (key comes from user-secrets)
namespace FoodOrderingSystem.Services;

// PB27 chatbot settings, read from the "Gemini" section of configuration.
// The API key is NEVER in appsettings.json or git: it lives in user-secrets on each PC
// (dotnet user-secrets set "Gemini:ApiKey" "...").
public class GeminiOptions
{
    public string ApiKey { get; set; } = string.Empty;

    // A stable Flash-Lite model: about 2 s per answer, and its free-tier limit is high enough for a demo.
    // (gemini-3.6-flash allows only 5 requests a minute on the free tier, and one question can take 2-3 requests
    // because of the tool round trip.) Override with "Gemini:Model" if Google retires it.
    public string Model { get; set; } = "gemini-3.5-flash-lite";

    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta";
}
