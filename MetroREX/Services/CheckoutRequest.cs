using MetroREX.Models;

namespace MetroREX.Services;

internal sealed class CheckoutRequest
{
    public UserProfile? GuestProfile { get; init; }

    public PaymentMethod PaymentMethod { get; init; }

    public CardDetails? Card { get; init; }

    public string? CouponCode { get; init; }
}
