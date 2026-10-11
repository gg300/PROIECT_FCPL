using MetroREX.Models;

namespace MetroREX.Services;

/// <param name="Discount">Reducerea data de cupon.</param>
/// <param name="SubscriptionDiscount">Reducerea data de abonamentele la vanzatori; se aduna cu cea a cuponului.</param>
internal sealed record OrderQuote(
    IReadOnlyList<OrderItem> Items,
    Coupon? Coupon,
    decimal Subtotal,
    decimal Discount,
    decimal SubscriptionDiscount)
{
    public decimal TotalDiscount => Discount + SubscriptionDiscount;

    public decimal Total => Subtotal - TotalDiscount;
}
