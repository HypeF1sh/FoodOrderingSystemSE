using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace FoodOrderingSystem.Services;

// Calls Gemini's REST API with a plain HttpClient (no SDK package). Streaming uses Server-Sent Events:
// with alt=sse Google sends one "data: {json}" line per chunk, so the answer can be shown while it is written.
public class GeminiClient : IGeminiClient
{
    private readonly HttpClient _httpClient;
    private readonly GeminiOptions _options;
    private readonly ILogger<GeminiClient> _logger;

    public GeminiClient(HttpClient httpClient, IOptions<GeminiOptions> options, ILogger<GeminiClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.ApiKey);

    public async IAsyncEnumerable<JsonObject> StreamAsync(JsonObject request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            throw new GeminiException("The chatbot is not set up on this computer yet.");
        }

        // 1. The key goes in a header, not the URL, so it never shows up in logs of the address
        var url = $"{_options.BaseUrl}/models/{_options.Model}:streamGenerateContent?alt=sse";
        using var message = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(request.ToJsonString(), Encoding.UTF8, "application/json")
        };
        message.Headers.Add("x-goog-api-key", _options.ApiKey);

        // 2. ResponseHeadersRead: start reading as soon as Google starts answering, not when it has finished
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Could not reach the Gemini API");
            throw new GeminiException("The chatbot cannot reach its AI service right now. Please try again later.");
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Gemini API returned {Status}: {Body}", (int)response.StatusCode, body);
                throw new GeminiException(DescribeFailure(response.StatusCode), ReadRetryDelay(body));
            }

            // 3. Read the stream line by line; each "data:" line is one complete JSON chunk
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream);

            string? line;
            while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
            {
                if (!line.StartsWith("data:", StringComparison.Ordinal)) continue;

                if (JsonNode.Parse(line.Substring(5).Trim()) is JsonObject chunk)
                {
                    yield return chunk;
                }
            }
        }
    }

    // A 429 error body says how long to wait, e.g. "retryDelay": "23s". Null when there is no such hint.
    private static TimeSpan? ReadRetryDelay(string body)
    {
        var match = Regex.Match(body, "\"retryDelay\"\\s*:\\s*\"(\\d+)(?:\\.\\d+)?s\"");
        return match.Success ? TimeSpan.FromSeconds(int.Parse(match.Groups[1].Value)) : null;
    }

    // Plain-language reasons; the full API error is in the server log for the developer
    private static string DescribeFailure(HttpStatusCode status) => status switch
    {
        HttpStatusCode.TooManyRequests => "The chatbot is busy right now. Please wait a minute and try again.",
        HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden =>
            "The chatbot's API key was not accepted. Please let the restaurant know.",
        HttpStatusCode.NotFound => "The chatbot's AI model is not available. Please let the restaurant know.",
        _ => "The chatbot's AI service had a problem. Please try again later."
    };
}
