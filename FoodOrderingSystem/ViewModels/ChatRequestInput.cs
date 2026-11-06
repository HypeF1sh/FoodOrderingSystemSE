// PB27 (Jordan) - view model: chat request input
using System.ComponentModel.DataAnnotations;

namespace FoodOrderingSystem.ViewModels;

// PB27: what the chat widget posts: the recent conversation, ending with the customer's new question
public class ChatRequestInput
{
    [Required]
    [MinLength(1)]
    [MaxLength(20, ErrorMessage = "The conversation is too long. Please start a new chat.")]
    public List<ChatMessageInput> Messages { get; set; } = new();
}
