namespace MetroREX.Models;

/// <summary>Un produs din coș, cu prețul de la momentul adăugării.</summary>
public class CartItem
{
    public string ProductId { get; set; } = "";
    public string ProductName { get; set; } = "";
    public string Category { get; set; } = "";
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }

    public decimal Total => UnitPrice * Quantity;
}
