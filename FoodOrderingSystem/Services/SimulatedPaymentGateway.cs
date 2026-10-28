using System.Security.Cryptography;
using FoodOrderingSystem.Models.Enums;
using FoodOrderingSystem.ViewModels;

namespace FoodOrderingSystem.Services;

// Stand-in for a real payment provider (chosen 22/10, spec §3). No money moves and nothing is stored.
// Two test values are declined so the failure path can be demonstrated; everything else is approved.
public class SimulatedPaymentGateway : IPaymentGateway
{
    public const string DeclinedCardNumber = "4000000000000002";
    public const string DeclinedWalletSuffix = "0000";

    public Task<(bool Approved, string Reference, string? DeclineReason)> ChargeAsync(decimal amount, PaymentInput details)
    {
        // A real provider replies with its own reference; ours is "SIM-" + date + 8 random hex characters
        var reference = $"SIM-{DateTime.Now:yyyyMMdd}-{Convert.ToHexString(RandomNumberGenerator.GetBytes(4))}";

        string? declineReason = null;
        if (details.Method == PaymentMethod.Card
            && PaymentInput.DigitsOnly(details.CardNumber) == DeclinedCardNumber)
        {
            declineReason = "Card declined by issuer.";
        }
        else if (details.Method == PaymentMethod.EWallet
                 && (details.WalletPhone ?? string.Empty).EndsWith(DeclinedWalletSuffix))
        {
            declineReason = "E-wallet has insufficient balance.";
        }

        // Task.FromResult: the interface is async because a real provider is a network call
        return Task.FromResult((declineReason == null, reference, declineReason));
    }
}
