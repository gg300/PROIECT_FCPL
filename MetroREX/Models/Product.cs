using System.Text.Json.Serialization;

namespace MetroREX.Models;

public class Product
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";

    public decimal BasePrice { get; set; }

    public decimal CurrentPrice { get; set; }

    public int Quantity { get; set; }

    public int QuantityYesterday { get; set; }

    public string Description { get; set; } = "";
    public string Seller { get; set; } = "";
    public List<string> Colors { get; set; } = new();

    public List<ProductSpec> Specs { get; set; } = new();

    public int RatingSum { get; set; }
    public int RatingCount { get; set; }

    [JsonIgnore]
    public double Rating => RatingCount == 0 ? 0 : (double)RatingSum / RatingCount;

    [JsonIgnore]
    public bool InStock => Quantity > 0;

    public void AddRating(int stars)
    {
        if (stars is < 1 or > 5) throw new ArgumentOutOfRangeException(nameof(stars), "Rating între 1 și 5.");
        RatingSum += stars;
        RatingCount++;
    }
}

public class ProductSpec
{
    public string Name { get; set; } = "";
    public string Value { get; set; } = "";
}
