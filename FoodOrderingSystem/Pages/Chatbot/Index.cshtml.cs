using System.Security.Claims;
using System.Text.Json;
using FoodOrderingSystem.Data;
using FoodOrderingSystem.Services;
using FoodOrderingSystem.ViewModels;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FoodOrderingSystem.Pages.Chatbot;

// PB27: the chat widget posts here and reads the answer back as a stream of Server-Sent Events
// ("data: {json}" lines), so the customer sees progress and the answer as it is written.
// Open to visitors too; only a logged-in customer's own bookings and orders can be looked up.
public class IndexModel : PageModel
{
    // Each visitor's browser session can ask this many questions, so nobody can run up the API bill
    private const int MaxQuestionsPerSession = 40;
    private const string CountKey = "ChatbotQuestions";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IChatbotService _chatbotService;

    public IndexModel(IChatbotService chatbotService)
    {
        _chatbotService = chatbotService;
    }

    // There is nothing to show at /Chatbot itself: the widget lives on every customer page
    public IActionResult OnGet() => RedirectToPage("/Index");

    // The anti-forgery token is checked as usual: the widget sends it in the RequestVerificationToken header
    public async Task<IActionResult> OnPostAsync([FromBody] ChatRequestInput input)
    {
        // 1. Server-Sent Events: tell the browser a stream is coming, and send each piece straight away
        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        HttpContext.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();

        // RequestAborted fires when the customer presses Stop or leaves the page
        var cancellationToken = HttpContext.RequestAborted;

        // 2. Field checks from the data annotations (message length, roles, history size)
        if (!ModelState.IsValid)
        {
            var problem = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage;
            await SendAsync(ChatStreamEvent.Error(string.IsNullOrEmpty(problem) ? "Please type a question." : problem), cancellationToken);
            return new EmptyResult();
        }

        // 3. Per-session limit
        var asked = HttpContext.Session.GetInt32(CountKey) ?? 0;
        if (asked >= MaxQuestionsPerSession)
        {
            await SendAsync(ChatStreamEvent.Error("You have reached the chat limit for now. Please call or ask our staff."), cancellationToken);
            return new EmptyResult();
        }
        HttpContext.Session.SetInt32(CountKey, asked + 1);

        // 4. Only customers have bookings and orders to look up; staff and visitors get general answers
        var customerId = User.IsInRole(DbSeeder.CustomerRole) ? User.FindFirstValue(ClaimTypes.NameIdentifier) : null;

        try
        {
            await foreach (var chatEvent in _chatbotService.AnswerAsync(input.Messages, customerId, cancellationToken))
            {
                await SendAsync(chatEvent, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Stop was pressed: the connection is already closed, so there is no one left to tell
        }

        // The answer has already been written to the response, so there is no page to render
        return new EmptyResult();
    }

    private async Task SendAsync(ChatStreamEvent chatEvent, CancellationToken cancellationToken)
    {
        await Response.WriteAsync($"data: {JsonSerializer.Serialize(chatEvent, JsonOptions)}\n\n", cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);
    }
}
