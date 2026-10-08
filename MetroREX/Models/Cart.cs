namespace MetroREX.Models;

/// <summary>Coșul de cumpărături al sesiunii curente (există și pentru vizitatori).</summary>
public class Cart
{
    public List<CartItem> Items { get; } = new();

    public decimal Subtotal => Items.Sum(i => i.Total);
    public bool IsEmpty => Items.Count == 0;

    /// <summary>Adaugă produsul; dacă există deja în coș, mărește cantitatea.</summary>
    public void Add(Product product, int quantity)
    {
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));

        var existing = Items.FirstOrDefault(i => i.ProductId == product.Id);
        if (existing is not null)
        {
            existing.Quantity += quantity;
            existing.UnitPrice = product.CurrentPrice;
            return;
        }

        Items.Add(new CartItem
        {
            ProductId = product.Id,
            ProductName = product.Name,
            Category = product.Category,
            UnitPrice = product.CurrentPrice,
            Quantity = quantity,
        });
    }

    public void Remove(string productId) => Items.RemoveAll(i => i.ProductId == productId);

    public void Clear() => Items.Clear();
}
