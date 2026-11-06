// PB27 (Jordan) - view model: chat stream event
namespace FoodOrderingSystem.ViewModels;

// PB27: one update sent to the browser while the bot works. Type is one of:
//   "status" - what the bot is doing now ("Checking the menu..."), shown as progress
//   "text"   - the next piece of the answer, appended as it streams in
//   "error"  - something went wrong; Text is safe to show the customer
//   "done"   - the answer is complete
public class ChatStreamEvent
{
    public string Type { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;

    public static ChatStreamEvent Status(string text) => new() { Type = "status", Text = text };
    public static ChatStreamEvent Chunk(string text) => new() { Type = "text", Text = text };
    public static ChatStreamEvent Error(string text) => new() { Type = "error", Text = text };
    public static ChatStreamEvent Done() => new() { Type = "done" };
}
