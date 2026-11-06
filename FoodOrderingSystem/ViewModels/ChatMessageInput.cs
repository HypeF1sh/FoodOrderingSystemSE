// PB27 (Jordan) - view model: chat message input
using System.ComponentModel.DataAnnotations;

namespace FoodOrderingSystem.ViewModels;

// PB27: one line of the conversation, as the chat widget sends it back with every question.
// The browser keeps the history, so the server stores nothing between messages.
public class ChatMessageInput
{
    // "user" for the customer, "assistant" for the bot
    [Required]
    [RegularExpression("^(user|assistant)$", ErrorMessage = "Role must be user or assistant.")]
    public string Role { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please type a question.")]
    [StringLength(1000, ErrorMessage = "Please keep each message under {1} characters.")]
    public string Text { get; set; } = string.Empty;
}
