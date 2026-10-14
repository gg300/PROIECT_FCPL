using MetroREX.Models;

namespace MetroREX.Services;

internal sealed record OrderQuote(
    IReadOnlyList<OrderItem> Items,
    Coupon? Coupon,
    decimal Subtotal,
    decimal Discount,
    decimal SubscriptionDiscount,
    int PointsUsed = 0,
    decimal PointsDiscount = 0m)
{
    public decimal TotalDiscount => Discount + SubscriptionDiscount + PointsDiscount;

    public decimal TotalBeforePoints => Subtotal - Discount - SubscriptionDiscount;

    public decimal Total => Subtotal - TotalDiscount;
}
