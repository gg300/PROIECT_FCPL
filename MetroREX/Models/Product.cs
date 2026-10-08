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

    public Product Clone()
    {
        var copy = new Product();
        copy.CopyFrom(this);
        return copy;
    }

    public void CopyFrom(Product other)
    {
        ArgumentNullException.ThrowIfNull(other);

        Id = other.Id;
        Name = other.Name;
        Category = other.Category;
        BasePrice = other.BasePrice;
        CurrentPrice = other.CurrentPrice;
        Quantity = other.Quantity;
        QuantityYesterday = other.QuantityYesterday;
        Description = other.Description;
        Seller = other.Seller;
        Colors = new List<string>(other.Colors);
        Specs = other.Specs.Select(spec => new ProductSpec { Name = spec.Name, Value = spec.Value }).ToList();
        RatingSum = other.RatingSum;
        RatingCount = other.RatingCount;
    }

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
