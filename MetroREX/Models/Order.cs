namespace MetroREX.Models;

/// <summary>Un produs dintr-o comandă (pentru istoric: preț × cantitate = total).</summary>
public class OrderItem
{
    public string ProductId { get; set; } = "";
    public string ProductName { get; set; } = "";
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }

    /// <summary>Nota 1–5 dată după livrare; null = încă nenotat.</summary>
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

    /// <summary>Datele de livrare de la momentul comenzii (și pentru vizitatori).</summary>
    public UserProfile Profile { get; set; } = new();

    public string? CouponCode { get; set; }
    public decimal DiscountAmount { get; set; }

    public decimal Subtotal => Items.Sum(i => i.Total);
    public decimal TotalCost => Subtotal - DiscountAmount;

    /// <summary>Livrată când data curentă >= data livrării.</summary>
    public bool IsDelivered(DateTime now) => now.Date >= DeliveryDate.Date;
}
