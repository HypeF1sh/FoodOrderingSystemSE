using System.ComponentModel.DataAnnotations;
using FoodOrderingSystem.Models.Enums;

namespace FoodOrderingSystem.ViewModels;

// What the customer types on the Pay page (PB16). There is deliberately no Amount field:
// the amount always comes from the order in the database, never from the browser.
// Card number and CVV are only held in memory for the gateway call and are never saved.
// Every field is [Required]; the Pay page drops the errors for the method the customer did not choose.
public class PaymentInput
{
    [Display(Name = "Payment method")]
    public PaymentMethod Method { get; set; } = PaymentMethod.Card;

    // ---- Card ----

    [Required(ErrorMessage = "Enter the name on the card.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Name on card must be 2 to 100 characters.")]
    [Display(Name = "Name on card")]
    public string? CardHolderName { get; set; }

    [Required(ErrorMessage = "Enter the card number.")]
    [RegularExpression(@"^[\d ]{13,23}$", ErrorMessage = "Card number must be 13 to 19 digits.")]
    [Display(Name = "Card number")]
    public string? CardNumber { get; set; }

    [Required(ErrorMessage = "Enter the expiry date.")]
    [RegularExpression(@"^(0[1-9]|1[0-2])/\d{2}$", ErrorMessage = "Use MM/YY, e.g. 08/28.")]
    [Display(Name = "Expiry (MM/YY)")]
    public string? Expiry { get; set; }

    [Required(ErrorMessage = "Enter the CVV.")]
    [RegularExpression(@"^\d{3,4}$", ErrorMessage = "CVV must be 3 or 4 digits.")]
    [Display(Name = "CVV")]
    public string? Cvv { get; set; }

    // ---- E-wallet ----

    [Required(ErrorMessage = "Enter your e-wallet phone number.")]
    [RegularExpression(@"^01\d{8,9}$", ErrorMessage = "Enter a Malaysian mobile number, e.g. 0123456789.")]
    [Display(Name = "E-wallet phone number")]
    public string? WalletPhone { get; set; }

    // Fields that belong to each method, so the Pay page can ignore the method that was not chosen
    public static readonly string[] CardFields = { nameof(CardHolderName), nameof(CardNumber), nameof(Expiry), nameof(Cvv) };
    public static readonly string[] WalletFields = { nameof(WalletPhone) };

    // "4242 4242 4242 4242" -> "4242424242424242"
    public static string DigitsOnly(string? value) =>
        new string((value ?? string.Empty).Where(char.IsDigit).ToArray());
}
