using MetroREX.Models;

namespace MetroREX.Services;

internal sealed record OrderQuote(IReadOnlyList<OrderItem> Items, Coupon? Coupon, decimal Subtotal, decimal Discount)
{
    public decimal Total => Subtotal - Discount;
}
