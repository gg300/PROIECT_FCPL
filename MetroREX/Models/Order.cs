namespace MetroREX.Models;

public class OrderItem
{
    public string ProductId { get; set; } = "";
    public string ProductName { get; set; } = "";
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }

    public int? Rating { get; set; }

    public decimal Total => UnitPrice * Quantity;
}

public class Order
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public DateTime OrderDate { get; set; }
    public DateTime DeliveryDate { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Active;
    public List<OrderItem> Items { get; set; } = new();

    public PaymentMethod PaymentMethod { get; set; }

    public UserProfile Profile { get; set; } = new();

    public string? CouponCode { get; set; }
    public decimal DiscountAmount { get; set; }

    public decimal Subtotal => Items.Sum(i => i.Total);
    public decimal TotalCost => Subtotal - DiscountAmount;

    public bool IsDelivered(DateTime now) => now.Date >= DeliveryDate.Date;
}
